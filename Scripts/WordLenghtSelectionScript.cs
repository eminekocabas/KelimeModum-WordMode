using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class WordLengthSelector : MonoBehaviour
{
    public Button[] buttons;
    public Color selectedColor;
    public Color normalColor;
    public float selectedScale = 1.15f;
    public float normalScale = 1f;
    public float scaleSpeed = 8f;
    public float duration = 0.5f;

    private Coroutine fadeCoroutine;

    public static int selectedLength;

    void Awake()
    {
        selectedLength = PlayerPrefs.GetInt("WordLength", 5);
    }

    void Start()
    {
        UpdateVisuals();
    }

    private void Update()
    {
        UpdateVisuals();
    }

    public void SelectLength(int length)
    {
        selectedLength = length;

        PlayerPrefs.SetInt("WordLength", length);
        PlayerPrefs.Save();

        UpdateVisuals();

    }

    IEnumerator FadeToColor(Color target, Image image)
    {
        Color startColor = image.color;
        float time = 0;

        while (time < duration)
        {
            time += Time.deltaTime;
            // Lerp fonksiyonu iki renk arasında yumuşak geçiş yapar
            image.color = Color.Lerp(startColor, target, time / duration);
            yield return null;
        }

        image.color = target; // Tam hedef renge sabitle
    }

    void UpdateVisuals()
    {
        foreach (Button btn in buttons)
        {
            if (!int.TryParse(btn.tag, out int length))
                continue;

            bool isSelected = (length == selectedLength);

            Image image = btn.GetComponentInChildren<Image>(true);

            Color targetColor = isSelected ? selectedColor : normalColor;

            // Eğer halihazırda bir geçiş varsa durdur ki çakışmasın
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);

            fadeCoroutine = StartCoroutine(FadeToColor(targetColor, image));



            float targetScale = isSelected ? selectedScale : normalScale;

            RectTransform rect = btn.GetComponent<RectTransform>();

            Vector3 currentScale = rect.localScale;
            Vector3 desiredScale = new Vector3(targetScale, targetScale, 1f);

            rect.localScale = Vector3.Lerp(
                currentScale,
                desiredScale,
                Time.deltaTime * scaleSpeed
            );
        }
    }
    //    if (length == selectedLength)
    //{
    //    btn.image.color = selectedColor;
    //}
    //else
    //{
    //    btn.image.color = normalColor;
    //}

    
}