using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class ImageScaler : MonoBehaviour
{
    public Image image;                  // Button child Image
    public float duration = 0.25f;       // animasyon süresi
    public float targetScaleX = 1.2f;    // X ekseninde büyüme
    public bool isSelected = false;      // örnek trigger

    void Start()
    {
        // Baþlangýçta scale = 0
        image.rectTransform.localScale = Vector3.zero;

        // Animasyonu baþlat
        StartCoroutine(ScaleFromZero(image.rectTransform, isSelected));
    }

    IEnumerator ScaleFromZero(RectTransform rt, bool expand)
    {
        float elapsed = 0f;

        Vector3 startScale = Vector3.zero;  // 0’dan baþla
        Vector3 targetScale = expand ? new Vector3(targetScaleX, 1f, 1f) : Vector3.zero;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.SmoothStep(0f, 1f, elapsed / duration); // yumuþak geçiþ

            rt.localScale = Vector3.Lerp(startScale, targetScale, t);

            yield return null;
        }

        rt.localScale = targetScale; // kesin son deðer
    }
}