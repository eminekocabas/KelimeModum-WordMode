using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HintManager : MonoBehaviour
{
    public KeyboardManager keyboardManager;
    public bool isDailyMode; // Inspector'dan veya kodla set edilmeli

    private DiamondCalculation diamondCalculation;
    private List<char> eliminatedLetters = new List<char>();
    private const string SAVE_KEY = "Daily_EliminatedLetters";
    private const string DATE_KEY = "Daily_HintDate";

    [Header("Zaman Ayarları")]
    [SerializeField] private float fadeInDuration = 0.5f;  // 0.2 çok hızlı olabilir, 0.5 dene
    [SerializeField] private float waitDuration = 1.5f;    // Ekranda kalma süresi
    [SerializeField] private float fadeOutDuration = 0.8f; // Kaybolma süresi

    [SerializeField]
    private CanvasGroup warningCanvasGroupDiamond;
    private Coroutine currentRoutine;


    void Start()
    {
        diamondCalculation = UnityEngine.Object.FindAnyObjectByType<DiamondCalculation>();

        if (isDailyMode)
        {
            CheckAndLoadDailyProgress();
        }

        if (warningCanvasGroupDiamond != null)
        {
            warningCanvasGroupDiamond.gameObject.SetActive(true);
            warningCanvasGroupDiamond.alpha = 0;
        }
    }

    /// <summary>
    /// Harf eleme mantığı. Mod durumuna göre kaydeder.
    /// </summary>
    public void Eliminate3Letters(string correctWord)
    {
        // Elmas harca
        diamondCalculation.SpendDiamond(2);

        if (DiamondCalculation.notEnougDiamond)
        {
            Debug.Log("Yetersiz Elmas!");
            ShowMessageDiamond();
            return;
        }

        List<Button> eliminatableButtons = new List<Button>();
        Button[] allButtons = GetComponentsInChildren<Button>(true);

        foreach (Button btn in allButtons)
        {
            if (!btn.interactable) continue;

            TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
            if (txt == null || string.IsNullOrEmpty(txt.text)) continue;

            char letter = txt.text[0];

            // Eğer harf doğru kelimede yoksa elenebilirler listesine ekle
            if (!correctWord.ToUpper().Contains(letter.ToString().ToUpper()))
            {
                eliminatableButtons.Add(btn);
            }
        }

        int countToEliminate = Mathf.Min(3, eliminatableButtons.Count);

        for (int i = 0; i < countToEliminate; i++)
        {
            int randomIndex = UnityEngine.Random.Range(0, eliminatableButtons.Count);
            Button targetButton = eliminatableButtons[randomIndex];
            char letter = targetButton.GetComponentInChildren<TMP_Text>().text[0];

            DisableButton(targetButton, letter);

            if (isDailyMode && !eliminatedLetters.Contains(letter))
            {
                eliminatedLetters.Add(letter);
            }

            eliminatableButtons.RemoveAt(randomIndex);
        }

        if (isDailyMode)
        {
            SaveEliminatedLetters();
        }
    }

    private void DisableButton(Button btn, char letter)
    {
        btn.interactable = false;
        keyboardManager.MarkLetterAsGray(letter);
    }

    #region Kayıt Sistemi (Persistence)

    private void CheckAndLoadDailyProgress()
    {
        string lastSavedDate = PlayerPrefs.GetString(DATE_KEY, "");
        string today = DateTime.Now.ToString("yyyy-MM-dd");

        // Eğer gün değişmişse eski verileri sil
        if (lastSavedDate != today)
        {
            ResetHintsForNewDay();
        }
        else
        {
            LoadEliminatedLetters();
        }
    }

    private void SaveEliminatedLetters()
    {
        string data = string.Join("", eliminatedLetters);
        PlayerPrefs.SetString(SAVE_KEY, data);
        PlayerPrefs.SetString(DATE_KEY, DateTime.Now.ToString("yyyy-MM-dd"));
        PlayerPrefs.Save();
    }

    private void LoadEliminatedLetters()
    {
        string savedData = PlayerPrefs.GetString(SAVE_KEY, "");
        if (string.IsNullOrEmpty(savedData)) return;

        eliminatedLetters = new List<char>(savedData.ToCharArray());
        Button[] allButtons = GetComponentsInChildren<Button>(true);

        foreach (char letter in eliminatedLetters)
        {
            foreach (Button btn in allButtons)
            {
                TMP_Text txt = btn.GetComponentInChildren<TMP_Text>();
                if (txt != null && txt.text.Length > 0 && txt.text[0] == letter)
                {
                    DisableButton(btn, letter);
                }
            }
        }
    }

    public void ResetHintsForNewDay()
    {
        PlayerPrefs.DeleteKey(SAVE_KEY);
        PlayerPrefs.DeleteKey(DATE_KEY);
        eliminatedLetters.Clear();
    }


    #endregion

    public void ShowMessageDiamond()
    {
        if (warningCanvasGroupDiamond == null)
        {
            Debug.Log("Canvas Group Null");
            return;
        }


        if (currentRoutine != null) StopCoroutine(currentRoutine);
        currentRoutine = StartCoroutine(FadeSequence(warningCanvasGroupDiamond));
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

        currentRoutine = null; // İşlem bittiğinde temizle
    }
}
