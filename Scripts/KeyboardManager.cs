using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI; // Image bileþeni için

public class KeyboardManager : MonoBehaviour
{
    public static KeyboardManager Instance { get; private set; }
    private Dictionary<string, Image> keyboardButtons = new Dictionary<string, Image>();

    [Header("Zaman Ayarlarý")]
    [SerializeField] private float fadeInDuration = 0.5f;  // 0.2 çok hýzlý olabilir, 0.5 dene
    [SerializeField] private float waitDuration = 1.5f;    // Ekranda kalma süresi
    [SerializeField] private float fadeOutDuration = 0.8f; // Kaybolma süresi

    [SerializeField] private Color darkGray = new Color(0.2f, 0.2f, 0.2f);
    [SerializeField] private Color green = new Color(0f, 0.9f, 0f);
    [SerializeField] private Color yellow = new Color(1f, 0.9f, 0f);

    public IGameManager gameScript;

    [Header("Uyarý Mesajý Ayarlarý")]
    [SerializeField] private CanvasGroup warningCanvasGroup;
    private Coroutine currentRoutine;
    // Harflerin o anki "en yüksek öncelikli" rengini tutar
    private Dictionary<string, Color> letterColors = new Dictionary<string, Color>();

    void Awake()
    {
        // Uyarý paneli kontrolü
        if (warningCanvasGroup != null)
        {
            warningCanvasGroup.gameObject.SetActive(true);
            warningCanvasGroup.alpha = 0;
        }

        

        // gameScript hala null ise sahnede ara
        if (gameScript == null)
        {
            // Sahnede IGameManager arayüzünü kullanan herhangi bir MonoBehaviour ara
            MonoBehaviour[] allScripts = Object.FindObjectsByType<MonoBehaviour>(FindObjectsSortMode.None);
            foreach (var script in allScripts)
            {
                if (script is IGameManager)
                {
                    gameScript = (IGameManager)script;
                    break;
                }
            }
        }

        // Eðer hala bulunamadýysa hata ver ki oyun çökmeden bilelim
        if (gameScript == null)
        {
            Debug.LogError("HATA: Sahnede IGameManager interface'ine sahip bir script bulunamadý! " +
                "GameManager script'inin baþýna 'public class GameManager : MonoBehaviour, IGameManager' yazdýðýndan emin ol.");
        }

        
    }

    void Start()
    {
        SetupKeyboard();

    }

    // Mesajý tetiklemek için bu metodu kullan
    public void ShowMessageHardMode()
    {
        if (warningCanvasGroup == null)
        {
            Debug.Log("Canvas Group Null");
            return;
        }
            

        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FadeSequence(warningCanvasGroup));
    }

    IEnumerator FadeSequence(CanvasGroup canvasGroup)
    {
        float timer = 0;

        // 1. Fade In
        while (timer < fadeInDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(0, 1, timer / fadeInDuration);
            yield return null;
        }
        canvasGroup.alpha = 1; // Tam görünürlük garantisi

        // 2. Bekleme
        yield return new WaitForSeconds(waitDuration);

        // 3. Fade Out
        timer = 0;
        while (timer < fadeOutDuration)
        {
            timer += Time.deltaTime;
            canvasGroup.alpha = Mathf.Lerp(1, 0, timer / fadeOutDuration);
            yield return null;
        }
        canvasGroup.alpha = 0; // Tam gizlilik garantisi

        currentRoutine = null; // Ýþlem bittiðinde temizle
    }


    public void MarkLetterAsGreen(char letter)
    {
        string l = letter.ToString().ToUpper();
        if (keyboardButtons.ContainsKey(l))
        {
            keyboardButtons[l].color = green;
            letterColors[l] = green; // Durumu güncelle
        }
    }

    public void MarkLetterAsYellow(char letter)
    {
        string l = letter.ToString().ToUpper();

        // EÐER ZATEN YEÞÝLSE, SARIYA ÇEVÝRME
        if (letterColors.ContainsKey(l) && letterColors[l] == green)
            return;

        if (keyboardButtons.ContainsKey(l))
        {
            keyboardButtons[l].color = yellow;
            letterColors[l] = yellow;
        }
    }

    public void MarkLetterAsGray(char letter)
    {
        string l = letter.ToString().ToUpper();

        // EÐER ZATEN YEÞÝL VEYA SARIYSA, GRÝYE ÇEVÝRME
        if (letterColors.ContainsKey(l) && (letterColors[l] == green || letterColors[l] == yellow))
            return;

        if (keyboardButtons.ContainsKey(l))
        {
            keyboardButtons[l].color = darkGray;
            letterColors[l] = darkGray;
        }
    }

    void SetupKeyboard()
    {
        Button[] allButtons = GetComponentsInChildren<Button>();

        foreach (Button btn in allButtons)
        {
            if (gameScript.GameEnded)
            {
                btn.interactable = false;
            }
            else
            {
                btn.interactable = true;

            }
            // Buton ismini al ve büyük harfe çevir (Örn: "Backspace", "Enter", "A")
            string btnName = btn.gameObject.name.ToUpper();

            // UI Text ayarlarý (Harf olsun olmasýn hepsine uygula)
            TextMeshProUGUI letterText = btn.GetComponentInChildren<TextMeshProUGUI>();
            if (letterText != null)
            {
                letterText.enableAutoSizing = true;
                letterText.fontSizeMin = 18;
                letterText.fontSizeMax = 40;
            }

            btn.onClick.RemoveAllListeners();

            // --- 1. HARFLER (Tek Karakterli Ýsimler: A, B, C...) ---
            if (btnName.Length == 1)
            {
                Image img = btn.GetComponent<Image>();
                if (img != null && !keyboardButtons.ContainsKey(btnName))
                {
                    keyboardButtons.Add(btnName, img);
                }

                // Closure hatasýný önlemek için deðiþkeni yerelleþtir
                string capturedLetter = btnName;
                btn.onClick.AddListener(() => {
                    // Burada doðrudan gameScript çaðýrmak yerine OnKeyClick'e gitmek daha güvenli
                    // Eðer listen yoksa þimdilik boþ gönderiyoruz
                    gameScript.AddLetter(letterText.text);
                });
            }
            // --- 2. ÖZEL BUTONLAR (BACKSPACE) ---
            else if (btnName == "BACKSPACE")
            {
                btn.onClick.AddListener(() => {
                    Debug.Log("Geri silme yapýlýyor...");
                    gameScript.DeleteLetter(); // IGameManager'da bu metodun olduðunu varsayýyorum
                });
            }
            // --- 3. ÖZEL BUTONLAR (ENTER) ---
            else if (btnName == "ENTER BUTTON")
            {
                btn.onClick.AddListener(() => {
                    Debug.Log("Kelime onaylanýyor...");
                    gameScript.SubmitGuess(); // IGameManager'da bu metodun olduðunu varsayýyorum
                });
            }

            else if (btnName == "CLEAR ALL BUTTON")
            {
                btn.onClick.AddListener(() => {
                    Debug.Log("Kelime onaylanýyor...");
                    gameScript.ClearRow(); // IGameManager'da bu metodun olduðunu varsayýyorum
                });
            }
        }
    }
}