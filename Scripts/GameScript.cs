using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using UnityEngine.UIElements;
using Button = UnityEngine.UI.Button;
using Image = UnityEngine.UI.Image;

public class GameScript : MonoBehaviour, IGameManager
{
    #region Data Structures
    [System.Serializable]
    public class LetterGroup { public string Harf; public string[] Kelimeler; }
    [System.Serializable]
    public class LetterGroupList { public LetterGroup[] groups; }
    #endregion

    #region References & UI
    [Header("Dependencies")]
    private LifeCalcullationScript lifeCalcullationScript;
    public TileFlipEffectScript flipEffect;
    private KeyboardManager keyboardManager;
    private HintManager hintManager;
    private DiamondCalculation diamondCalculation;

    [Header("JSON Files")]
    [SerializeField] private TextAsset answersFile;
    [SerializeField] private TextAsset validWordsFile;

    [Header("UI Grid Settings")]
    [SerializeField] private Transform gridParent;
    private TMP_Text[][] allRows;
    public List<char> grayLetters = new List<char>();
    public Button eliminateButton;
    public Button revealButton;


    [Header("End Screens")]
    public TMP_Text gameOverWordText;
    public TMP_Text congratsWordText;
    public GameObject gameOverScreen, congractsScreen, sparkleEffectCanvas;
    #endregion

    #region Game State
    private bool gameEnded = false;
    public bool GameEnded => gameEnded;
    private bool isProcessing = false;
    private int currentRow = 0;
    private int currentIndex = 0;
    private string currentGuess = "";
    private int numGuess = 0;
    private string correctWord;
    private int lifeRemained;
    private int diamondRemained;
    private HashSet<string> validGuesses;
    private LetterGroupList wordData;
    List<string> usedWords = new List<string>();

    [Header("Settings")]
    public int wordLength;
    private const int totalGuessLimit = 6;
    private const int baseMultiplier = 9;

    public bool win = false;
    public bool Win => win;

    private readonly CultureInfo tr = new CultureInfo("tr-TR");
    #endregion

    #region Unity Lifecycle
    void Awake()
    {
        SetupGrid();
        keyboardManager = Object.FindAnyObjectByType<KeyboardManager>();
        lifeCalcullationScript = Object.FindAnyObjectByType<LifeCalcullationScript>();
        hintManager = Object.FindAnyObjectByType<HintManager>();
        diamondCalculation = Object.FindAnyObjectByType<DiamondCalculation>();

    }

    void Start()
    {
        LoadData();
        SelectRandomWord();
        if (sparkleEffectCanvas != null) sparkleEffectCanvas.SetActive(false);

        lifeRemained = PlayerPrefs.GetInt("lifeRemained", 5);
        diamondRemained = PlayerPrefs.GetInt("diamondRemained", 0);
        ButtonManager.giveUp = false;

        eliminateButton.onClick.AddListener(() =>
        {
            hintManager.Eliminate3Letters(correctWord);
        });

        revealButton.onClick.AddListener(() =>
        {
            RevealRandomLetter(correctWord.Length, correctWord, allRows, currentRow);
        });

    }

    void Update()
    {
        if (gameEnded || isProcessing) return;

        // Can kontrolü ve Give Up kontrolü
        if (lifeRemained <= 0) { EndGame(false); return; }
        if (ButtonManager.giveUp) { EndGame(false); return; }

        HandlePhysicalInput();
    }
    #endregion
    #region Hint / IPUCU TRACKING
    private bool[][] hintLetters; // Her satır için hangi hücre ipucu ile açıldı
    #endregion

    #region Grid Setup
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

    private void SyncCurrentGuess()
    {
        char[] guessChars = new char[wordLength];
        for (int i = 0; i < wordLength; i++)
        {
            // Eğer hücrede ipucu veya oyuncu harfi varsa onu al
            string text = allRows[currentRow][i].text;
            guessChars[i] = string.IsNullOrEmpty(text) ? ' ' : text[0];
        }
        currentGuess = new string(guessChars);
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
            SyncCurrentGuess();
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
        SyncCurrentGuess();

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

        //if (diamondRemained >= 5)
        //{ diamondRemained = diamondRemained - 5; }
        //else
        //{
        //    Debug.Log("Not Enough Diamonds");
        //    return;
        //}

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

    #region Input & Submission
    void HandlePhysicalInput()
    {
        foreach (char c in Input.inputString)
        {
            if (char.IsLetter(c)) AddLetter(c.ToString());
        }

        if (Input.GetKeyDown(KeyCode.Backspace)) DeleteLetter();
        if (Input.GetKeyDown(KeyCode.Return)) SubmitGuess();
    }

    
    public void SubmitGuess()
    {
        if (gameEnded || isProcessing || currentGuess.Length != wordLength) return;

        // Geçerlilik Kontrolü
        if (!validGuesses.Contains(currentGuess))
        {
            StartCoroutine(ShakeRow(currentRow));
            return;
        }

        // Hard Mode Kontrolü: Tahminin içinde gri harf var mı?
        if (SceneLoader.HardMode)
        {
            foreach (char c in currentGuess)
            {
                if (grayLetters.Contains(c))
                {
                    // KeyboardManager.Instance?.ShowMessage(); // Klavye üzerinden uyarı ver
                    StartCoroutine(ShakeRow(currentRow));
                    return;
                }
            }
        }

        StartCoroutine(ProcessGuessRoutine(currentGuess));
    }
    #endregion

    #region Core Game Logic
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
            currentRow++;
            currentIndex = 0;
            currentGuess = "";
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
                if (grayLetters.Contains(guess[i]))
                {
                    // keyboardManager.floatingTextPrefab
                }
            }

            flipEffect.Flip(targetColor, tileImage, tileImage.rectTransform);
            yield return new WaitForSeconds(0.2f);
        }
          
    }
    #endregion

    #region UI & Effects
    IEnumerator ShakeRow(int row)
    {
        isProcessing = true;
        int len = allRows[row].Length;
        Image[] images = new Image[len];
        Vector3[] origins = new Vector3[len];

        for (int i = 0; i < len; i++)
        {
            // Kutucuğun Image bileşenini alıyoruz
            images[i] = allRows[row][i].GetComponentInParent<Image>();
            origins[i] = images[i].rectTransform.localPosition;
        }

        float elapsed = 0f;
        float speed = 6f; // 0.4 saniyede 2 kez yanıp sönmesi için hızı biraz artırdım

        while (elapsed < 0.4f)
        {
            elapsed += Time.deltaTime;

            // PingPong değeri 0 ile 1 arasında gidip gelir
            float t = Mathf.PingPong(elapsed * speed, 1f);
            Color blinkColor = Color.Lerp(Color.white, Color.red, t);

            float xOffset = Random.Range(-8f, 8f);

            for (int i = 0; i < len; i++)
            {
                // Sarsıntı pozisyonu
                images[i].rectTransform.localPosition = origins[i] + new Vector3(xOffset, 0, 0);

                // İŞTE BURASI: Kutucuğun (Image) rengini değiştiriyoruz
                images[i].color = blinkColor;
            }

            yield return null;
        }

        // İşlem bitince her şeyi eski haline (Beyaz ve orijinal pozisyon) döndür
        for (int i = 0; i < len; i++)
        {
            images[i].rectTransform.localPosition = origins[i];
            images[i].color = Color.white;
        }

        ClearRow();
        isProcessing = false;
    }

  
    void EndGame(bool isWin)
    {
        gameEnded = true;

        if (isWin)
        {
            congratsWordText.text = correctWord;
            congractsScreen.SetActive(true);
            if (sparkleEffectCanvas != null)  sparkleEffectCanvas.SetActive(true);
            // PlayerPrefs.SetInt("Total Points", PlayerPrefs.GetInt("Total Points", 0) + CalculatePoints());

            var stats = StatsService.Data;

            // 2. İstatistikleri güncelle
            stats.totalWins++;

            // Tahmin dağılımını güncelle (Örn: 3. tahminde bildiyse dizinin 3. elemanını artır)
            // Dizi indeksi 0'dan başladığı için -1 yapıyoruz
            if (numGuess > 0 && numGuess <= stats.guessDistribution.Length)
            {
                stats.guessDistribution[numGuess - 1]++;
            }

            StatsService.Save();

            Debug.Log("İstatistikler kaydedildi! Toplam galibiyet: " + stats.totalWins);
        }
        else
        {
            gameOverWordText.text = correctWord;
            lifeCalcullationScript.DecreaseLife();
           // if (PlayerPrefs.GetInt("lifeRemained") <= 0) enoughLifePanel.SetActive(true);
            gameOverScreen.SetActive(true);
        }
        PlayerPrefs.Save();
    }

    int CalculatePoints()
    {
        int p = (totalGuessLimit + 1 - numGuess) * baseMultiplier;
        return SceneLoader.HardMode ? p * 2 : p;
    }

    public void ClearRow()
    {
        // Satırdaki her bir hücreyi (index bazlı) gezelim
        for (int i = 0; i < wordLength; i++)
        {
            // Eğer bu hücre ipucu ile açılmamışsa içini temizle
            if (!hintLetters[currentRow][i])
            {
                allRows[currentRow][i].text = "";
            }
        }

        currentIndex = 0;
        currentGuess = "";

        while (currentIndex < wordLength && hintLetters[currentRow][currentIndex])
        {
            currentIndex++;
        }
    }
    #endregion

    public static class GameData
    {
        // Tüm sahnelerde ortak olan gri harfler
        public static HashSet<char> GlobalGrayLetters = new HashSet<char>();

        public static void Reset() => GlobalGrayLetters.Clear();
    }

    #region Data Loading
    void LoadData()
    {
        wordData = JsonUtility.FromJson<LetterGroupList>("{ \"groups\": " + answersFile.text + " }");
        var validData = JsonUtility.FromJson<LetterGroupList>("{ \"groups\": " + validWordsFile.text + " }");
        validGuesses = new HashSet<string>();
        foreach (var g in validData.groups)
            foreach (string w in g.Kelimeler) validGuesses.Add(w.ToUpper(tr));
    }

    void SelectRandomWord()
    {
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
                    SelectRandomWord();
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


}