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
    #endregion

    #region References
    [Header("Dependencies")]
    public LifeCalcullationScript lifeCalcullationScript;
    public TileFlipEffectScript flipEffect;
    private StreakCalculation streakScript;
    private KeyboardManager keyboardManager;
    private ButtonManager buttonManager;

    [Header("JSON Files")]
    [SerializeField] private TextAsset answersFile;
    [SerializeField] private TextAsset validWordsFile;

    [Header("UI Grid Settings")]
    [SerializeField] private Transform gridParent;
    private TMP_Text[][] allRows;
    public TMP_Text gameOverWordText, congratsWordText;
    public GameObject gameOverScreen, congractsScreen;
    public Button giveUpButton;
    #endregion

    #region Game State
    private bool gameEnded = false;
    private bool isProcessing = false;
    private int currentRow = 0;
    private int currentIndex = 0;
    private int numGuess = 0;

    [Header("Settings")]
    public int wordLength;
    private const int totalGuessLimit = 6;
    private const int baseMultiplier = 30;

    private string correctWord;
    private string currentGuess = "";
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

    void Start()
    {
        SetupGrid();
        //var allStrikes = Resources.FindObjectsOfTypeAll<StreakCalculation>();
        //Debug.Log("Sahnede toplam bulunan script sayısı: " + allStrikes.Length);
        streakScript = Object.FindAnyObjectByType<StreakCalculation>();
        keyboardManager = Object.FindAnyObjectByType<KeyboardManager>();
        lifeCalcullationScript = Object.FindAnyObjectByType<LifeCalcullationScript>();
        // Start içinde kullanımı:
        


        if (streakScript == null)
        {
            Debug.Log("streakScript bulunamadı");

        }

        LoadData();
        InitializeDailyGame();
    }

    void Update()
    {
        if (gameEnded || isProcessing) return;

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
        int index = rng.Next(total);

        foreach (var group in wordData.groups)
        {
            if (index < group.Kelimeler.Length)
            {
                correctWord = group.Kelimeler[index].ToUpper(tr);
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

        char upperChar = char.ToUpper(letter[0], tr);
        if (grayLetters.Contains(upperChar) && SceneLoader.HardMode)
        {
            keyboardManager.ShowMessage();
            return;
            // keyboardManager.floatingTextPrefab
        }
        currentGuess += upperChar;

        allRows[currentRow][currentIndex].text = upperChar.ToString();
        currentIndex++;

    }

    public void DeleteLetter()
    {
        if (gameEnded || isProcessing || currentIndex <= 0) return;

        currentIndex--;
        currentGuess = currentGuess.Substring(0, currentGuess.Length - 1);
        allRows[currentRow][currentIndex].text = "";
    }

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
            HandleWin();
        }
        else if (numGuess >= totalGuessLimit)
        {
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
        PlayerPrefs.SetInt(PlayedKey, 1);
        PlayerPrefs.SetInt(WinKey, 1);

        // Puan ve Seri
        int score = (totalGuessLimit + 1 - numGuess) * baseMultiplier;
        if (SceneLoader.HardMode) score *= 2;

        PlayerPrefs.SetInt("Total Points", PlayerPrefs.GetInt("Total Points", 0) + score);
        streakScript.UpdateStreak();

        PlayerPrefs.Save();
        GlobalGameManager.Instance.ReportWin(this.wordLength);
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
        for (int i = 0; i < wordLength; i++) allRows[currentRow][i].text = "";
        currentIndex = 0;
        currentGuess = "";
    }
    #endregion

    #region Grid Setup
    void SetupGrid()
    {
        int rowCount = gridParent.childCount;
        allRows = new TMP_Text[rowCount][];

        for (int i = 0; i < rowCount; i++)
        {
            Transform rowTransform = gridParent.GetChild(i);
            // Sadece doğrudan çocukları alarak daha güvenli hale getiriyoruz
            allRows[i] = rowTransform.GetComponentsInChildren<TMP_Text>();
        }
    }
    #endregion
}