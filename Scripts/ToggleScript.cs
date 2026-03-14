using UnityEngine;
using UnityEngine.UI;

public class ToggleManager : MonoBehaviour
{
    public static ToggleManager Instance;

    [Header("Hard Mode Toggle")]
    public Toggle hardModeToggle;
    public GameObject x2Image;

    [Header("Switch Visuals")]
    public RectTransform handle;
    public float moveAmount = 100f;
    public float speed = 10f;

   // public Color onColor;
   // public Color offColor;

    Vector2 onPos;
    Vector2 offPos;

    Image background;

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            //DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
            return;
        }
    }

    void Start()
    {
        // Arkaplan (Toggle'ın Image component'i)
        background = hardModeToggle.GetComponent<Image>();

        // Handle pozisyonları
        offPos = handle.anchoredPosition;
        onPos = offPos + new Vector2(moveAmount, 0);

        // PlayerPrefs'ten Hard Mode oku
        bool hardModeOn = PlayerPrefs.GetInt("HardMode", 0) == 1;
        hardModeToggle.isOn = hardModeOn;

        // Event
        hardModeToggle.onValueChanged.AddListener(OnHardModeToggleChanged);

        // İlk görsel güncelleme
        UpdateVisualInstant();
    }

    void Update()
    {
        AnimateSwitch();
    }

    // HARD MODE
    public void OnHardModeToggleChanged(bool value)
    {

        PlayerPrefs.SetInt("HardMode", value ? 1 : 0);
        PlayerPrefs.Save();

        UpdateVisualColor(value);
    }

    // Switch animasyonu
    void AnimateSwitch()
    {

        Vector2 target = hardModeToggle.isOn ? onPos : offPos;

        if (!(x2Image == null))
        {
            x2Image.gameObject.SetActive(hardModeToggle.isOn);

        }
        else
        {
            return;
        }


            handle.anchoredPosition = Vector2.Lerp(
                handle.anchoredPosition,
                target,
                Time.deltaTime * speed
            );
    }

    // Renk güncelle
    void UpdateVisualColor(bool isOn)
    {
        //background.color = isOn ? onColor : offColor;
    }

    // İlk açılışta anında doğru hale getir
    void UpdateVisualInstant()
    {
        bool isOn = hardModeToggle.isOn;
       // background.color = isOn ? onColor : offColor;
        handle.anchoredPosition = isOn ? onPos : offPos;

        x2Image.gameObject.SetActive(isOn);

    }
}