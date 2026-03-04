using UnityEngine;
using System;
using System.Linq;

public class GlobalGameManager : MonoBehaviour
{
    public static GlobalGameManager Instance { get; private set; }

    private string TodayKey => DateTime.Now.ToString("yyyyMMdd");
    // Takip edilecek modlarýn anahtarlarý
    private readonly string[] modeKeys = { "Mode_4", "Mode_5", "Mode_6"};

    void Awake()
    {
        if (Instance == null) { Instance = this; DontDestroyOnLoad(gameObject); }
        else { Destroy(gameObject); }
    }

    // Herhangi bir mod kazanýldýðýnda çaðrýlýr
    public void ReportWin(int wordLength)
    {
        // 1. Bugün en az bir galibiyet alýndýðýný kaydet
        PlayerPrefs.SetInt("HasAnyWinToday_" + TodayKey, 1);

        // 2. Hangi modun kazanýldýðýný iþaretle (Ýsteðe baðlý istatistik için)
        PlayerPrefs.SetInt($"Win_Mode_{wordLength}_{TodayKey}", 1);

        // 3. Streak'i güncelle
        StreakCalculation streakScript = UnityEngine.Object.FindAnyObjectByType<StreakCalculation>();
        if (streakScript != null)
        {
            streakScript.UpdateStreak();
        }

        PlayerPrefs.Save();
        Debug.Log($"Galibiyet Raporlandý: {wordLength} harfli mod.");
    }

    // Bir mod kaybedildiðinde (tüm haklar bittiðinde) çaðrýlýr
    public void ReportLoss(int wordLength)
    {
        // Bu spesifik modun bugün kaybedildiðini iþaretle
        PlayerPrefs.SetInt($"Mode_{wordLength}_Loss_{TodayKey}", 1);

        // KONTROL: Eðer oyuncu bugün baþka bir modda ZATEN kazandýysa, streak bozulmaz.
        if (PlayerPrefs.GetInt("HasAnyWinToday_" + TodayKey, 0) == 1)
        {
            Debug.Log("Bir mod kaybedildi ama bugün zaten bir galibiyet var. Streak korunuyor.");
            return;
        }

        // KONTROL: Eðer oyuncu 4, 5, 6 ve 7 harfli modlarýn hepsini denemiþ ve hepsini kaybetmiþse
        if (CheckIfAllAttemptsFailed())
        {
            ResetStreak();
        }

        PlayerPrefs.Save();
    }

    private bool CheckIfAllAttemptsFailed()
    {
        // Eðer 4 modun dördünde de "Loss" bayraðý kalkmýþsa true döner
        return modeKeys.All(mode => PlayerPrefs.GetInt(mode + "_Loss_" + TodayKey, 0) == 1);
    }

    private void ResetStreak()
    {
        Debug.Log("Tüm harf modlarýnda baþarýsýz olundu. Streak sýfýrlanýyor.");
        PlayerPrefs.SetInt("CurrentStreak", 0);
        PlayerPrefs.SetString("LastWinDate", "");
        PlayerPrefs.Save();
    }
}