using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class TileFlipEffectScript : MonoBehaviour
{
    public float selectedScale = 1.1f;
    public float normalScale = 1f;
    public float scaleSpeed = 8f; // büyüme hızı
    public float duration = 1f;
    public void Flip(Color targetColor, Image tileImage, RectTransform rect)
    {

        StartCoroutine(FlipRoutine(targetColor, tileImage, rect));
    }

    IEnumerator FlipRoutine(Color targetColor, Image tileImage, RectTransform rect)
    {
        float half = duration / 2f;
        float t = 0f;

        Vector3 startScale = Vector3.one;
        rect.localScale = startScale;

        while (t < half)
        {
            float y = Mathf.Lerp(1f, 0f, t / half);
            rect.localScale = new Vector3(1f, y, 1f);
            t += Time.deltaTime;
            yield return null;
        }

        tileImage.color = targetColor;

        t = 0f;

        while (t < half)
        {
            float y = Mathf.Lerp(0f, 1f, t / half);
            rect.localScale = new Vector3(1f, y, 1f);
            t += Time.deltaTime;
            yield return null;
        }

        rect.localScale = Vector3.one;
    }
}