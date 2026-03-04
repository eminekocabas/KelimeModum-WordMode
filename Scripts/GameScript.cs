using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

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

    [Header("JSON Files")]
    [SerializeField] private TextAsset answersFile;
    [SerializeField] private TextAsset validWordsFile;

    [Header("UI Grid Settings")]
    [SerializeField] private Transform gridParent;
    private TMP_Text[][] allRows;
    public List<char> grayLetters = new List<char>();


    [Header("End Screens")]
    public TMP_Text gameOverWordText;
    public TMP_Text congratsWordText;
    public GameObject gameOverScreen, congractsScreen, enoughLifePanel;
    #endregion

    #region Game State
    private bool gameEnded = false;
    private bool isProcessing = false;
    private int currentRow = 0;
    private int currentIndex = 0;
    private string currentGuess = "";
    private int numGuess = 0;
    private string correctWord;
    private int lifeRemained;
    private HashSet<string> validGuesses;
    private LetterGroupList wordData;

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

    }

    void Start()
    {
        LoadData();
        SelectRandomWord();
        lifeRemained = PlayerPrefs.GetInt("lifeRemained", 5);

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
            EndGame(true);
        }
        else if (numGuess >= totalGuessLimit)
        {
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
            PlayerPrefs.SetInt("Total Points", PlayerPrefs.GetInt("Total Points", 0) + CalculatePoints());
        }
        else
        {
            gameOverWordText.text = correctWord;
            lifeCalcullationScript.DecreaseLife();
            if (PlayerPrefs.GetInt("lifeRemained") <= 0) enoughLifePanel.SetActive(true);
            else gameOverScreen.SetActive(true);
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
        foreach (var t in allRows[currentRow]) t.text = "";
        currentIndex = 0; currentGuess = "";
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
                correctWord = group.Kelimeler[index].ToUpper(tr);
                return;
            }
            index -= group.Kelimeler.Length;
        }
    }
    #endregion


}
