using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameScript_DailyWord : MonoBehaviour, IGameManager
{
    #region Data Structures
    [System.Serializable]
    public class LetterGroup { public string Harf; public string[] Kelimeler; }
    [System.Serializable]
    public class LetterGroupList { public LetterGroup[] groups; }
    private bool[][] hintLetters; // Her satır için hangi hücre ipucu ile açıldı

    #endregion

    #region References
    [Header("Dependencies")]
    public LifeCalcullationScript lifeCalcullationScript;
    public TileFlipEffectScript flipEffect;
    private StreakCalculation streakScript;
    private KeyboardManager keyboardManager;
    // private ButtonManager buttonManager;
    private DiamondCalculation diamondCalculation;
    private HintManager hintManager;

    [Header("JSON Files")]
    [SerializeField] private TextAsset answersFile;
    [SerializeField] private TextAsset validWordsFile;
    List<string> usedWords = new List<string>();


    [Header("UI Grid Settings")]
    [SerializeField] private Transform gridParent;
    private TMP_Text[][] allRows;
    public TMP_Text gameOverWordText, congratsWordText;
    public GameObject gameOverScreen, congractsScreen, sparkelEffectCanvas;
    public Button eliminateButton,revealButton;
    #endregion

    #region Game State
    private bool gameEnded = false;
    public bool GameEnded => gameEnded;

    private bool isProcessing = false;
    private int currentRow = 0;
    private int currentIndex = 0;
    private int numGuess = 0;

    [Header("Settings")]
    public int wordLength;
    private const int totalGuessLimit = 6;
    private const int baseMultiplier = 30;

    private static string correctWord;
    public string currentGuess = "";
    private HashSet<string> validGuesses;
    private LetterGroupList wordData;
    public static List<char> grayLetters = new List<char>();
    private CultureInfo tr = new CultureInfo("tr-TR");

    public bool win = false;
    public bool Win => win;
    #endregion

    #region Daily Keys
    private string TodayKey => System.DateTime.Now.ToString("yyyyMMdd");
    private string ModeKey => $"L{wordLength}";
    private string WordKey => $"DailyWord_{TodayKey}_{ModeKey}";
    private string GuessKey => $"DailyGuesses_{TodayKey}_{ModeKey}";
    private string PlayedKey => $"DailyPlayed_{TodayKey}_{ModeKey}";
    private string WinKey => $"DailyWin_{TodayKey}_{ModeKey}";
    #endregion

    #region Unity Lifecycle

    private void Awake()
    {
        sparkelEffectCanvas = GameObject.FindWithTag("Effects Canvas");

        if (sparkelEffectCanvas == null) Debug.Log("canvas atanmadı");

        if (sparkelEffectCanvas != null) sparkelEffectCanvas.SetActive(false);

        ButtonManager.giveUp = false;
        SetupGrid();
        
        streakScript = Object.FindAnyObjectByType<StreakCalculation>();
        keyboardManager = Object.FindAnyObjectByType<KeyboardManager>();
        lifeCalcullationScript = Object.FindAnyObjectByType<LifeCalcullationScript>();        
        diamondCalculation = Object.FindAnyObjectByType<DiamondCalculation>();
        hintManager = Object.FindAnyObjectByType<HintManager>();

        eliminateButton.onClick.AddListener(() =>
        {
            hintManager.Eliminate3Letters(correctWord);
        });

        revealButton.onClick.AddListener(() =>
        {
            RevealRandomLetter(correctWord.Length, correctWord, allRows, currentRow);
        });

        if (streakScript == null)
        {
            Debug.Log("streakScript bulunamadı");

        }

        //LoadData();
        //InitializeDailyGame();
        //LoadRevealedLetters(wordLength, correctWord, allRows, currentRow);
    }

    void Start()
    {
        LoadData();
        InitializeDailyGame();
        LoadRevealedLetters(wordLength, correctWord, allRows, currentRow);
    }

    void Update()
    {
        if (gameEnded || isProcessing)
        {

            return;
        }

        if (ButtonManager.giveUp)
        {
            HandleLoss();
            return;
        }

        HandleInput();
    }
    #endregion

    #region Initialization
    void LoadData()
    {
        wordData = JsonUtility.FromJson<LetterGroupList>("{ \"groups\": " + answersFile.text + " }");
        validGuesses = new HashSet<string>();
        var validData = JsonUtility.FromJson<LetterGroupList>("{ \"groups\": " + validWordsFile.text + " }");
        foreach (var g in validData.groups)
            foreach (string w in g.Kelimeler) validGuesses.Add(w.ToUpper(tr));
    }

    void InitializeDailyGame()
    {
        // Kelime Belirleme
        if (PlayerPrefs.HasKey(WordKey))
            correctWord = PlayerPrefs.GetString(WordKey);
        else
        {
            SelectDailyWord();
            PlayerPrefs.SetString(WordKey, correctWord);
        }

        // Oyun Durumu Kontrolü
        if (PlayerPrefs.GetInt(PlayedKey, 0) == 1)
        {
            gameEnded = true;
            LoadDailyGuessesInstant();
            ShowDailyResult();
        }
        else
        {
            StartCoroutine(LoadDailyGuessesRoutine());
        }
    }

    void SelectDailyWord()
    {
        long seed = long.Parse(TodayKey + wordLength);
        System.Random rng = new System.Random((int)(seed % int.MaxValue));

        int total = wordData.groups.Sum(g => g.Kelimeler.Length);

        int index = Random.Range(0, total);

        foreach (var group in wordData.groups)
        {
            if (index < group.Kelimeler.Length)
            {
                string word = group.Kelimeler[index].ToUpper(tr);

                // Eğer kelime daha önce kullanıldıysa tekrar seç
                if (usedWords.Contains(word) && usedWords.Count < total)
                {
                    SelectDailyWord();
                    return;
                }

                correctWord = word;
                usedWords.Add(word);
                return;
            }

            index -= group.Kelimeler.Length;
        }


    }
    #endregion

    #region Input Management
    void HandleInput()
    {
        foreach (char c in Input.inputString)
        {
            if (char.IsLetter(c)) AddLetter(c.ToString());
        }

        if (Input.GetKeyDown(KeyCode.Backspace)) DeleteLetter();
        if (Input.GetKeyDown(KeyCode.Return)) SubmitGuess();
    }

    public void AddLetter(string letter)
    {
        if (gameEnded || isProcessing || currentIndex >= wordLength) return;

        // ÖNEMLİ: Eğer mevcut hücre ipucu ile dolmuşsa, bir sonraki boş hücreye atla
        while (currentIndex < wordLength && hintLetters[currentRow][currentIndex])
        {
            currentIndex++;
        }

        // Eğer atlamalar sonucu satır sonuna geldiysek yazma
        if (currentIndex >= wordLength) return;

        char upperChar = char.ToUpper(letter[0], tr);

        // Hard Mode Kontrolü
        if (grayLetters.Contains(upperChar) && SceneLoader.HardMode)
        {
            keyboardManager.ShowMessageHardMode();
            return;
        }

        currentGuess = UpdateGuessString(currentIndex, upperChar); // String'i güncelle
        allRows[currentRow][currentIndex].text = upperChar.ToString();

        // Bir sonraki harfe geçmeden önce tekrar ipucu kontrolü
        currentIndex++;
        while (currentIndex < wordLength && hintLetters[currentRow][currentIndex])
        {
            currentIndex++;
        }
    }

    public void DeleteLetter()
    {
        if (gameEnded || isProcessing || currentIndex <= 0) return;

        // Mevcut indeksi bir geri al, ama eğer ipucu harfiyse daha da geri git
        int targetIndex = currentIndex - 1;
        while (targetIndex >= 0 && hintLetters[currentRow][targetIndex])
        {
            targetIndex--;
        }

        if (targetIndex < 0) return; // Silinecek harf kalmadı (hepsi ipucu)

        allRows[currentRow][targetIndex].text = "";
        currentIndex = targetIndex; // İndeksi silinen yere çek

        currentGuess = currentGuess.Remove(targetIndex, 1).Insert(targetIndex, " ");
    }

    private string UpdateGuessString(int index, char letter)
    {
        char[] chars = currentGuess.PadRight(wordLength).ToCharArray();
        chars[index] = letter;
        return new string(chars).Replace("\0", " ");
    }

    #region Reveal / IPUCU
    private const string REVEAL_KEY = "Daily_RevealedIndexes";

    public void RevealRandomLetter(int wordLength, string correctWord, TMP_Text[][] allRows, int currentRow)
    {
        diamondCalculation.SpendDiamond(5);

        if (DiamondCalculation.notEnougDiamond)
        {
            hintManager.ShowMessageDiamond();
            return;
        }


        List<int> availableIndexes = new List<int>();

        for (int i = 0; i < wordLength; i++)
        {
            // Eğer hücre boşsa veya yanlış harf varsa seçilebilir
            if (allRows[currentRow][i].text != correctWord[i].ToString())
                availableIndexes.Add(i);
        }

        if (availableIndexes.Count == 0) return;

        int randomIndex = availableIndexes[UnityEngine.Random.Range(0, availableIndexes.Count)];

        // Harfi uygula
        ApplyHintLetter(randomIndex, correctWord, allRows, currentRow);

        // İndeksi kaydet
        SaveRevealedIndex(randomIndex);

        // Otomatik kazanma kontrolü
        CheckAutoWin(wordLength, correctWord, allRows, currentRow);
    }

    // Harfi hem görsel hem mantıksal olarak işleyen yardımcı fonksiyon
    private void ApplyHintLetter(int index, string correctWord, TMP_Text[][] allRows, int currentRow)
    {
        char hintLetter = correctWord[index];
        allRows[currentRow][index].text = hintLetter.ToString();

        // Senin sistemindeki kilit mekanizması
        if (hintLetters != null)
            hintLetters[currentRow][index] = true;

        Image tileImage = allRows[currentRow][index].GetComponentInParent<Image>();
        if (tileImage != null) tileImage.color = Color.green;

        keyboardManager.MarkLetterAsGreen(hintLetter);
    }

    #region Kayıt ve Yükleme

    private void SaveRevealedIndex(int index)
    {
        string currentData = PlayerPrefs.GetString(REVEAL_KEY, "");
        if (string.IsNullOrEmpty(currentData))
        {
            currentData = index.ToString();
        }
        else
        {
            currentData += "," + index; // İndeksleri virgülle ayırarak tut (Örn: "0,2,4")
        }
        PlayerPrefs.SetString(REVEAL_KEY, currentData);
        PlayerPrefs.Save();
    }

    // LoadEliminatedLetters içinde veya Start'ta çağrılmalı
    public void LoadRevealedLetters(int wordLength, string correctWord, TMP_Text[][] allRows, int currentRow)
    {
        string savedData = PlayerPrefs.GetString(REVEAL_KEY, "");
        if (string.IsNullOrEmpty(savedData)) return;

        string[] indexes = savedData.Split(',');
        foreach (string idxStr in indexes)
        {
            if (int.TryParse(idxStr, out int idx))
            {
                if (idx < wordLength)
                {
                    ApplyHintLetter(idx, correctWord, allRows, currentRow);
                }
            }
        }

        // Yükleme sonrası kazanma kontrolü
        CheckAutoWin(wordLength, correctWord, allRows, currentRow);
    }

    private void CheckAutoWin(int wordLength, string correctWord, TMP_Text[][] allRows, int currentRow)
    {
        bool allRevealed = true;
        for (int i = 0; i < wordLength; i++)
        {
            if (allRows[currentRow][i].text != correctWord[i].ToString())
            {
                allRevealed = false;
                break;
            }
        }

        if (allRevealed)
        {
            // HandleWin metodun nerede tanımlıysa oraya referans verilmeli
            // Eğer bu script içindeyse direkt çağır:
            HandleWin();
        }
    }

    #endregion
    #endregion
    public void SubmitGuess()
    {
        if (gameEnded || isProcessing || currentGuess.Length != wordLength) return;

        if (!validGuesses.Contains(currentGuess) ||
            (SceneLoader.HardMode && currentGuess.Any(x => grayLetters.Contains(x))))
        {
            StartCoroutine(ShakeRow(currentRow));
            return;
        }

        StartCoroutine(ProcessGuess(currentGuess, false));
    }
    #endregion

    #region Game Logic
    IEnumerator ProcessGuess(string guess, bool fromLoad)
    {
        isProcessing = true;

        yield return StartCoroutine(FlipRowSequentially(guess));

        numGuess++;
        if (!fromLoad) SaveDailyGuess(guess);

        if (guess == correctWord)
        {
            yield return new WaitForSeconds(1.5f);

            HandleWin();
        }
        else if (numGuess >= totalGuessLimit)
        {
            yield return new WaitForSeconds(1.5f);

            HandleLoss();
        }
        else
        {
            PrepareNextRow();
            isProcessing = false;
        }
    }

    IEnumerator FlipRowSequentially(string guess)
    {
        List<char> tempWord = new List<char>(correctWord);
        bool[] isGreen = new bool[wordLength];

        for (int i = 0; i < wordLength; i++)
        {
            if (guess[i] == correctWord[i])
            {
                isGreen[i] = true;
                tempWord.Remove(guess[i]);
            }
        }

        for (int i = 0; i < wordLength; i++)
        {
            Image tileImage = allRows[currentRow][i].GetComponentInParent<Image>();
            Color targetColor = Color.gray;

            if (isGreen[i])
            {
                targetColor = Color.green;
                keyboardManager.MarkLetterAsGreen(guess[i]);

            }
            else if (tempWord.Contains(guess[i]))
            {
                targetColor = Color.yellow;
                tempWord.Remove(guess[i]);
                keyboardManager.MarkLetterAsYellow(guess[i]);

            }
            else if (!correctWord.Contains(guess[i]))
            {
                grayLetters.Add(guess[i]);
                keyboardManager.MarkLetterAsGray(guess[i]);
            }

            flipEffect.Flip(targetColor, tileImage, tileImage.rectTransform);
            yield return new WaitForSeconds(0.15f);
        }
        yield return new WaitForSeconds(0.2f);
    }

    void PrepareNextRow()
    {
        currentRow++;
        currentIndex = 0;
        currentGuess = "";
    }

    void HandleWin()
    {
        gameEnded = true;
        win = true;
        if (sparkelEffectCanvas != null) sparkelEffectCanvas.SetActive(true);
        PlayerPrefs.SetInt(PlayedKey, 1);
        PlayerPrefs.SetInt(WinKey, 1);

        // Puan ve Seri
        int score = (totalGuessLimit + 1 - numGuess) * baseMultiplier;
        if (SceneLoader.HardMode) score *= 2;

        PlayerPrefs.SetInt("Total Points", PlayerPrefs.GetInt("Total Points", 0) + score);
        streakScript.UpdateStreak();

        PlayerPrefs.Save();
        if (GlobalGameManager.Instance == null) Debug.Log("Instance Null");
        GlobalGameManager.Instance.ReportWin(this.wordLength);
        var stats = StatsService.Data;
        stats.totalWins++;
        if (numGuess > 0 && numGuess <= stats.guessDistribution.Length)
        {
            stats.guessDistribution[numGuess - 1]++;
        }

        StatsService.Save();

        Debug.Log("İstatistikler kaydedildi! Toplam galibiyet: " + stats.totalWins);
        ShowDailyResult();
    }

    void HandleLoss()
    {
        gameEnded = true;
        PlayerPrefs.SetInt(PlayedKey, 1);
        PlayerPrefs.SetInt(WinKey, 0);
        lifeCalcullationScript.DecreaseLife();
        PlayerPrefs.Save();
        GlobalGameManager.Instance.ReportLoss(this.wordLength);
        ShowDailyResult();
    }

    void ShowDailyResult()
    {
        bool isWin = PlayerPrefs.GetInt(WinKey, 0) == 1;
        if (isWin)
        {
            congratsWordText.text = correctWord;
            congractsScreen.SetActive(true);
            if (sparkelEffectCanvas != null) sparkelEffectCanvas.SetActive(true);
        }
        else
        {
            gameOverWordText.text = correctWord;
            gameOverScreen.SetActive(true);
        }
    }
    #endregion

    #region Save & Load
    void SaveDailyGuess(string guess)
    {
        string data = PlayerPrefs.GetString(GuessKey, "");
        PlayerPrefs.SetString(GuessKey, data + guess + "|");
        PlayerPrefs.Save();
    }

    IEnumerator LoadDailyGuessesRoutine()
    {
        if (!PlayerPrefs.HasKey(GuessKey)) yield break;
        string[] guesses = PlayerPrefs.GetString(GuessKey).Split('|', System.StringSplitOptions.RemoveEmptyEntries);

        foreach (string g in guesses)
        {
            for (int i = 0; i < g.Length; i++) allRows[currentRow][i].text = g[i].ToString();
            yield return StartCoroutine(ProcessGuess(g, true));
        }
    }

    void LoadDailyGuessesInstant()
    {
        if (!PlayerPrefs.HasKey(GuessKey)) return;
        string[] guesses = PlayerPrefs.GetString(GuessKey).Split('|', System.StringSplitOptions.RemoveEmptyEntries);

        for (int r = 0; r < guesses.Length && r < totalGuessLimit; r++)
        {
            string g = guesses[r];
            List<char> tempWord = new List<char>(correctWord);

            // Renkleri önceden hesapla (Green'ler önce)
            bool[] greenCheck = new bool[g.Length];
            for (int i = 0; i < g.Length; i++) if (g[i] == correctWord[i]) { greenCheck[i] = true; tempWord.Remove(g[i]); }

            for (int i = 0; i < g.Length; i++)
            {
                allRows[r][i].text = g[i].ToString();
                Image img = allRows[r][i].GetComponentInParent<Image>();

                if (greenCheck[i]) img.color = Color.green;
                else if (tempWord.Contains(g[i])) { img.color = Color.yellow; tempWord.Remove(g[i]); }
                else img.color = Color.gray;
            }
        }
    }
    #endregion

    #region Effects
    IEnumerator ShakeRow(int row)
    {
        isProcessing = true;
        Image[] rowImages = new Image[wordLength];
        Vector3[] originalPos = new Vector3[wordLength];

        for (int i = 0; i < wordLength; i++)
        {
            rowImages[i] = allRows[row][i].GetComponentInParent<Image>();
            originalPos[i] = rowImages[i].rectTransform.localPosition;
        }

        float elapsed = 0f;
        while (elapsed < 0.5f)
        {
            float xOffset = Random.Range(-1f, 1f) * 10f;
            Color shakeColor = Color.Lerp(Color.white, new Color(1f, 0.5f, 0.5f), Mathf.PingPong(elapsed * 10, 1));
            foreach (var img in rowImages)
            {
                img.rectTransform.localPosition += new Vector3(xOffset, 0, 0);
                img.color = shakeColor;
            }
            elapsed += Time.deltaTime;
            yield return null;
            for (int i = 0; i < wordLength; i++) rowImages[i].rectTransform.localPosition = originalPos[i];
        }

        foreach (var img in rowImages) img.color = Color.white;
        ClearRow();
        isProcessing = false;
    }

    public void ClearRow()
    {
        char[] newGuess = new char[wordLength];

        for (int i = 0; i < wordLength; i++)
        {
            if (hintLetters[currentRow][i])
            {
                // Hint harfleri currentGuess içinde de tut
                newGuess[i] = allRows[currentRow][i].text[0];
            }
            else
            {
                // Boş hücreleri temizle
                allRows[currentRow][i].text = "";
                newGuess[i] = ' '; // boşluk olarak bırak
            }
        }

        // Güncellenmiş currentGuess
        currentGuess = new string(newGuess);

        // İndeksi ilk boş hücreye ayarla
        currentIndex = 0;
        while (currentIndex < wordLength && hintLetters[currentRow][currentIndex])
        {
            currentIndex++;
        }
    }
    #endregion

    #region Grid Setup
    void SetupGrid()
    {
        int rowCount = gridParent.childCount;
        allRows = new TMP_Text[rowCount][];
        hintLetters = new bool[rowCount][];   // EKLE

        for (int i = 0; i < rowCount; i++)
        {
            Transform rowTransform = gridParent.GetChild(i);

            allRows[i] = rowTransform.GetComponentsInChildren<TMP_Text>();

            // Her satır için hint dizisi oluştur
            hintLetters[i] = new bool[allRows[i].Length];
        }
    }
    #endregion
}