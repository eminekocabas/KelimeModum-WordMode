using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class GameScriptTimeLimitMode : MonoBehaviour, IGameManager
{
    #region Data Structures
    [System.Serializable]
    public class LetterGroup { public string Harf; public string[] Kelimeler; }
    [System.Serializable]
    public class LetterGroupList { public LetterGroup[] groups; }
    #endregion

    #region References
    [Header("Dependencies")]
    private LifeCalcullationScript lifeCalcullationScript;
    public TileFlipEffectScript flipEffect;
    private KeyboardManager keyboardManager;


    [Header("JSON Files")]
    [SerializeField] private TextAsset answersFile;
    [SerializeField] private TextAsset validWordsFile;

    [Header("UI Grid Settings")]
    [SerializeField] private Transform gridParent;
    private TMP_Text[][] allRows;

    [Header("UI Screens & Texts")]
    public TMP_Text gameOverWordText;
    public TMP_Text congratsWordText;
    public TMP_Text timerText;
    public GameObject gameOverScreen, congractsScreen, enoughLifePanel;
    #endregion

    #region Game State
    [Header("Settings")]
    public int wordLength = 5;
    public int timeLimitSeconds = 25;
    private const int totalGuessLimit = 6;
    private const int scoreTimeBase = 25;

    private float timeLeft;
    private bool gameEnded = false;
    private bool isProcessing = false;
    private int currentRow = 0;
    private int currentIndex = 1;
    private int numGuess = 0;
    private int lifeRemained;
    public Button tryAgainButton;


    private string correctWord;
    private string currentGuess = "";
    private string theFirstLetter;

    private HashSet<string> validGuesses;
    private Coroutine timerCoroutine;
    public List<char> grayLetters = new List<char>();
    private CultureInfo tr = new CultureInfo("tr-TR");

    public bool win = false;
    public bool Win => win;
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        if (timerText == null || gridParent == null)
        {
            SetUIs();
        }

        SetupGrid();
        lifeRemained = PlayerPrefs.GetInt("lifeRemained");
        keyboardManager = Object.FindAnyObjectByType<KeyboardManager>();
        lifeCalcullationScript = Object.FindAnyObjectByType<LifeCalcullationScript>();

        LoadData();
        InitializeGame();
    }

    void Update()
    {
        if (gameEnded || isProcessing) return;

        if (lifeRemained <= 0)
        {
            Debug.Log("Not Enough Lives.");
            EndGame(true);

        }

        if (ButtonManager.giveUp)
        {
            EndGame(true);
            return;
        }
        HandleInput();
    }
    #endregion

    #region Initialization
    void LoadData()
    {
        // Kelime listelerini yükle
        var answersData = JsonUtility.FromJson<LetterGroupList>("{ \"groups\": " + answersFile.text + " }");
        var validData = JsonUtility.FromJson<LetterGroupList>("{ \"groups\": " + validWordsFile.text + " }");

        validGuesses = new HashSet<string>();
        foreach (var g in validData.groups)
            foreach (string w in g.Kelimeler) validGuesses.Add(w.ToUpper(tr));

        // Rastgele kelime seç
        int total = answersData.groups.Sum(g => g.Kelimeler.Length);
        int index = Random.Range(0, total);

        foreach (var g in answersData.groups)
        {
            if (index < g.Kelimeler.Length)
            {
                correctWord = g.Kelimeler[index].ToUpper(tr);
                break;
            }
            index -= g.Kelimeler.Length;
        }
    }

    void InitializeGame()
    {
        theFirstLetter = correctWord[0].ToString();
        currentGuess = theFirstLetter;

        // İlk hücreyi ayarla
        allRows[0][0].text = theFirstLetter;
        allRows[0][0].color = Color.blue;

        StartTimer();
    }
    #endregion

    #region Timer Logic
    void StartTimer()
    {
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);
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

        if (!gameEnded) EndGame(false); // Süre biterse kaybet
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
        if (gameEnded || isProcessing || currentIndex <= 1) return; // İlk harfi silemez

        currentIndex--;
        currentGuess = currentGuess.Substring(0, currentGuess.Length - 1);
        allRows[currentRow][currentIndex].text = "";
    }

    public void SubmitGuess()
    {
        if (gameEnded || isProcessing || currentGuess.Length != wordLength) return;

        // Geçerlilik kontrolü
        if (!validGuesses.Contains(currentGuess) ||
            (SceneLoader.HardMode && currentGuess.Any(x => grayLetters.Contains(x))))
        {
            StartCoroutine(ShakeRow(currentRow));
            return;
        }

        StartCoroutine(ProcessGuessRoutine(currentGuess));
    }
    #endregion

    #region Core Logic
    IEnumerator ProcessGuessRoutine(string guess)
    {
        isProcessing = true;

        yield return StartCoroutine(FlipRowSequentially(guess));

        numGuess++;

        if (guess == correctWord)
        {
            HandleWin();
        }
        else if (numGuess >= totalGuessLimit)
        {
            EndGame(false);
        }
        else
        {
            PrepareNextRow();
            StartTimer(); // Her başarılı tahminde süreyi sıfırla
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

    void PrepareNextRow()
    {
        currentRow++;
        currentIndex = 1;
        currentGuess = theFirstLetter;

        allRows[currentRow][0].text = theFirstLetter;
        allRows[currentRow][0].color = Color.blue;
    }

    void HandleWin()
    {
        win = true;

        // Puan hesaplama
        int score = (totalGuessLimit + 1 - numGuess) * (scoreTimeBase + 1 - timeLimitSeconds);
        if (SceneLoader.HardMode) score *= 2;

        PlayerPrefs.SetInt("Total Points", PlayerPrefs.GetInt("Total Points", 0) + score);
        PlayerPrefs.Save();

        EndGame(true);
    }

    void EndGame(bool isWin)
    {
        gameEnded = true;
        isProcessing = false;
        if (timerCoroutine != null) StopCoroutine(timerCoroutine);

        if (isWin)
        {
            congratsWordText.text = correctWord;
            congractsScreen.SetActive(true);
        }
        else
        {
            gameOverWordText.text = correctWord;
            lifeCalcullationScript.DecreaseLife();
            if (tryAgainButton != null && PlayerPrefs.GetInt("lifeRemained") <= 0)
            {
                tryAgainButton.gameObject.SetActive(false);
                enoughLifePanel.SetActive(true);
            }
            else
            {
                gameOverScreen.SetActive(true);

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
            Color shakeColor = Color.Lerp(Color.white, Color.red, Mathf.PingPong(elapsed * 10, 1));

            for (int i = 0; i < wordLength; i++)
            {
                rowImages[i].rectTransform.localPosition = originalPos[i] + new Vector3(xOffset, 0, 0);
                rowImages[i].color = shakeColor;
            }
            elapsed += Time.deltaTime;
            yield return null;
        }

        for (int i = 0; i < wordLength; i++)
        {
            rowImages[i].rectTransform.localPosition = originalPos[i];
            rowImages[i].color = Color.white;
        }

        ClearRow();
        isProcessing = false;
    }

    public void ClearRow()
    {
        for (int i = 1; i < wordLength; i++) allRows[currentRow][i].text = "";
        currentGuess = theFirstLetter;
        currentIndex = 1;
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

    void SetUIs()
    {
        TextMeshProUGUI[] alltextsInScene = Resources.FindObjectsOfTypeAll<TextMeshProUGUI>();

        foreach (TextMeshProUGUI txt in alltextsInScene)
        {
            if (txt.name == "Countdown Text")
            {
                timerText = txt;
               //timerText.gameObject.SetActive(SceneLoader.HardMode);
              //  Debug.Log("buton bulundu");
            }
        }

        Transform[] allObjectsInScene = Resources.FindObjectsOfTypeAll<Transform>();
        foreach (Transform obj in allObjectsInScene)
        {
            if (obj.name == "All Guesses")
            {
                gridParent = obj;
                //timerText.gameObject.SetActive(SceneLoader.HardMode);
                //  Debug.Log("buton bulundu");
            }
        }

    }
}