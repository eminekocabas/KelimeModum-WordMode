using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
public class GameScriptTimeLimitMode : MonoBehaviour, IGameManager
{
    #region Data Structures
    [System.Serializable]
    public class LetterGroup
    {
        public string Harf;
        public string[] Kelimeler;
    }

    [System.Serializable]
    public class LetterGroupList
    {
        public LetterGroup[] groups;
    }
    #endregion

    #region References
    [Header("Dependencies")]
    private LifeCalcullationScript lifeCalcullationScript;
    private TileFlipEffectScript flipEffect;
    private KeyboardManager keyboardManager;
    private DiamondCalculation diamondCalculation;
    private HintManager hintManager;


    [Header("JSON Files")]
    [SerializeField] private TextAsset answersFile;
    [SerializeField] private TextAsset validWordsFile;

    [Header("UI Grid")]
    [SerializeField] private Transform gridParent;
    private TMP_Text[][] allRows;

    [Header("UI")]
    public TMP_Text gameOverWordText;
    public TMP_Text congratsWordText;
    public TMP_Text timerText;

    public GameObject gameOverScreen;
    public GameObject congractsScreen;
    public GameObject sparkleEffectCanvas;


    public Button revealButton, eliminateButton;

    // public Button tryAgainButton;
    #endregion

    #region Game State
    [Header("Settings")]
    public int wordLength = 5;
    public int timeLimitSeconds = 25;

    private const int totalGuessLimit = 6;
    private const int scoreTimeBase = 25;

    private float timeLeft;
    private bool gameEnded = false;

    public bool GameEnded => gameEnded;
    private bool isProcessing = false;

    private int currentRow = 0;
    private int currentIndex = 1;
    private int numGuess = 0;
    private int lifeRemained;

    private string correctWord;
    private string currentGuess = "";
    private string theFirstLetter;

    private HashSet<string> validGuesses;
    private HashSet<string> usedWords = new HashSet<string>();

    private LetterGroupList answersData;

    private Coroutine timerCoroutine;

    public List<char> grayLetters = new List<char>();

    private CultureInfo tr = new CultureInfo("tr-TR");

    public bool win = false;
    public bool Win => win;
    #endregion

    #region Unity
    void Start()
    {
        ButtonManager.giveUp = false;

        SetupGrid();

        keyboardManager = Object.FindAnyObjectByType<KeyboardManager>();
        lifeCalcullationScript = Object.FindAnyObjectByType<LifeCalcullationScript>();
        diamondCalculation = Object.FindAnyObjectByType<DiamondCalculation>();
        flipEffect = Object.FindAnyObjectByType<TileFlipEffectScript>();
        hintManager = Object.FindAnyObjectByType<HintManager>();

        if (sparkleEffectCanvas != null) sparkleEffectCanvas.SetActive(false);

            eliminateButton.onClick.AddListener(() =>
        {
            hintManager.Eliminate3Letters(correctWord);
        });

        revealButton.onClick.AddListener(() =>
        {
            RevealRandomLetter(correctWord.Length, correctWord, allRows, currentRow);
        });

        lifeRemained = PlayerPrefs.GetInt("lifeRemained");

        LoadData();
        InitializeGame();
    }

    void Update()
    {
        if (gameEnded || isProcessing) return;

        if (lifeRemained <= 0)
        {
            EndGame(false);
            
            return;
        }

        if (ButtonManager.giveUp)
        {
            EndGame(false);
            return;
        }

        HandleInput();
    }
    #endregion

    #region Data Loading
    void LoadData()
    {
        answersData = JsonUtility.FromJson<LetterGroupList>("{ \"groups\": " + answersFile.text + " }");
        var validData = JsonUtility.FromJson<LetterGroupList>("{ \"groups\": " + validWordsFile.text + " }");

        validGuesses = new HashSet<string>();

        foreach (var g in validData.groups)
            foreach (string w in g.Kelimeler)
                validGuesses.Add(w.ToUpper(tr));

        SelectRandomWord();
    }

    void SelectRandomWord()
    {
        List<string> availableWords = new List<string>();

        foreach (var g in answersData.groups)
        {
            foreach (var w in g.Kelimeler)
            {
                string word = w.ToUpper(tr);

                if (!usedWords.Contains(word))
                    availableWords.Add(word);
            }
        }

        if (availableWords.Count == 0)
        {
            usedWords.Clear();
            SelectRandomWord();
            return;
        }

        correctWord = availableWords[Random.Range(0, availableWords.Count)];
        usedWords.Add(correctWord);
    }
    #endregion

    #region Game Init
    void InitializeGame()
    {
        theFirstLetter = correctWord[0].ToString();
        currentGuess = theFirstLetter;

        allRows[0][0].text = theFirstLetter;
        allRows[0][0].color = Color.blue;

        StartTimer();
    }
    #endregion

    #region Timer
    void StartTimer()
    {
        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        timeLeft = timeLimitSeconds;
        timerCoroutine = StartCoroutine(CountdownTimer());
    }

    IEnumerator CountdownTimer()
    {
        while (timeLeft > 0 && !gameEnded)
        {
            timerText.text = Mathf.Ceil(timeLeft).ToString();
            yield return new WaitForSeconds(1f);
            timeLeft--;
        }

        if (!gameEnded)
            EndGame(false);
    }
    #endregion

    #region Input
    void HandleInput()
    {
        foreach (char c in Input.inputString)
        {
            if (char.IsLetter(c))
                AddLetter(c.ToString());
        }

        if (Input.GetKeyDown(KeyCode.Backspace))
            DeleteLetter();

        if (Input.GetKeyDown(KeyCode.Return))
            SubmitGuess();
    }
    private bool[][] hintLetters; // Her satır için hangi hücre ipucu ile açıldı

    void SetupGrid()
    {
        int rowCount = gridParent.childCount;
        allRows = new TMP_Text[rowCount][];
        hintLetters = new bool[rowCount][]; // Hint durum dizisi

        for (int i = 0; i < rowCount; i++)
        {
            Transform rowTransform = gridParent.GetChild(i);
            allRows[i] = rowTransform.GetComponentsInChildren<TMP_Text>();
            hintLetters[i] = new bool[allRows[i].Length]; // Başlangıçta tüm hücreler false
        }
    }
    #endregion

    #region Input & Submission (Revize Add/Delete)
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

        // Tahmin string'inden o karakteri temizle (Örn: "K_LEM")
        currentGuess = currentGuess.Remove(targetIndex, 1).Insert(targetIndex, " ");
    }

    private string UpdateGuessString(int index, char letter)
    {
        char[] chars = currentGuess.PadRight(wordLength).ToCharArray();
        chars[index] = letter;
        return new string(chars).Replace("\0", " ");
    }
    #endregion

    #region Reveal / IPUCU
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
            if (allRows[currentRow][i].text != correctWord[i].ToString())
                availableIndexes.Add(i);
        }

        if (availableIndexes.Count == 0) return;

        int randomIndex = availableIndexes[Random.Range(0, availableIndexes.Count)];
        char hintLetter = correctWord[randomIndex];

        // IPUCU HARFİ KİLİTLE
        allRows[currentRow][randomIndex].text = hintLetter.ToString();
        hintLetters[currentRow][randomIndex] = true; // artık değiştirilemez

        Image tileImage = allRows[currentRow][randomIndex].GetComponentInParent<Image>();
        tileImage.color = Color.green;

        keyboardManager.MarkLetterAsGreen(hintLetter);

        // Eğer tüm harfler açıldıysa otomatik kazan
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
            EndGame(true);
        }
    }
    #endregion

    #region Guess
    public void SubmitGuess()
    {
        if (currentGuess.Length != wordLength)
            return;

        if (!validGuesses.Contains(currentGuess))
        {
            StartCoroutine(ShakeRow(currentRow));
            return;
        }

        StartCoroutine(ProcessGuessRoutine(currentGuess));
    }

    IEnumerator ProcessGuessRoutine(string guess)
    {
        isProcessing = true;

        yield return StartCoroutine(FlipRowSequentially(guess));

        numGuess++;

        if (guess == correctWord)
        {
            yield return new WaitForSeconds(1.5f);
            EndGame(true);

        }
        else if (numGuess >= totalGuessLimit)
        {
            yield return new WaitForSeconds(1.5f);

            EndGame(false);
        }
        else
        {
            PrepareNextRow();
            StartTimer();
            isProcessing = false;
        }
    }
    #endregion

    #region Flip Logic
    IEnumerator FlipRowSequentially(string guess)
    {
        List<char> tempWord = new List<char>(correctWord);
        bool[] isGreen = new bool[wordLength];

        for (int i = 0; i < wordLength; i++)
        {
            if (guess[i] == correctWord[i])
            {
                isGreen[i] = true;
                keyboardManager.MarkLetterAsGreen(guess[i]);
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
                
            }
            else if (tempWord.Contains(guess[i]))
            {
                targetColor = Color.yellow;
                tempWord.Remove(guess[i]);
                keyboardManager.MarkLetterAsYellow(guess[i]);
            }
            else
            {
                targetColor = Color.gray;
                grayLetters.Add(guess[i]);
                keyboardManager.MarkLetterAsGray(guess[i]);
            }

            flipEffect.Flip(targetColor, tileImage, tileImage.rectTransform);

            yield return new WaitForSeconds(0.2f);
        }
    }
    #endregion

    #region Row Logic
    void PrepareNextRow()
    {
        currentRow++;

        currentIndex = 1;
        currentGuess = theFirstLetter;

        allRows[currentRow][0].text = theFirstLetter;
        allRows[currentRow][0].color = Color.blue;
    }

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
        for (int i = 1; i < wordLength; i++)
            allRows[currentRow][i].text = "";

        currentGuess = theFirstLetter;
        currentIndex = 1;
    }
    #endregion

    #region End Game

    void EndGame(bool isWin)
    {
        gameEnded = true;

        if (timerCoroutine != null)
            StopCoroutine(timerCoroutine);

        if (isWin)
        {
            congratsWordText.text = correctWord;
            congractsScreen.SetActive(true);
            if (sparkleEffectCanvas != null) sparkleEffectCanvas.SetActive(true);

            var stats = StatsService.Data;

            // 2. İstatistikleri güncelle
            stats.totalWins++;

            // Tahmin dağılımını güncelle (Örn: 3. tahminde bildiyse dizinin 3. elemanını artır)
            // Dizi indeksi 0'dan başladığı için -1 yapıyoruz
            if (numGuess > 0 && numGuess <= stats.guessDistribution.Length)
            {
                stats.guessDistribution[numGuess - 1]++;
            }

            // 3. Veriyi diske kaydet (Kalıcı hale getir)
            StatsService.Save();

            Debug.Log("İstatistikler kaydedildi! Toplam galibiyet: " + stats.totalWins);
        }
        else
        {
            gameOverWordText.text = correctWord;

            lifeCalcullationScript.DecreaseLife();

            gameOverScreen.SetActive(true);

            //if (PlayerPrefs.GetInt("lifeRemained") <= 0)
            //{
            //   // tryAgainButton.gameObject.SetActive(false);
            //    enoughLifePanel.SetActive(true);
            //}
            //else
            //{
            //}
        }
    }
    #endregion
}