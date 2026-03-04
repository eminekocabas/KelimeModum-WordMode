using UnityEngine;
using System;
using System.Globalization;

public class StreakCalculation : MonoBehaviour
{
    public static int currentStreak;
    public static int bestStreak;

    private const string CurrentStreakKey = "CurrentStreak";
    private const string BestStreakKey = "BestStreak";
    private const string LastWinDateKey = "LastWinDate_Global";

    private const string DateFormat = "yyyyMMdd";

    void Awake()
    {
        LoadStats();
        CheckStreakValidity();

    }


    void LoadStats()
    {
        currentStreak = PlayerPrefs.GetInt(CurrentStreakKey, 0);
       // Debug.Log("currentStreak: " + currentStreak);
        bestStreak = PlayerPrefs.GetInt(BestStreakKey, 0);
    }

    void CheckStreakValidity()
    {
        if (!PlayerPrefs.HasKey(LastWinDateKey)) return;

        string savedDate = PlayerPrefs.GetString(LastWinDateKey);

        if (!DateTime.TryParseExact(
            savedDate,
            DateFormat,
            CultureInfo.InvariantCulture,
            DateTimeStyles.None,
            out DateTime lastWinDate))
        {
            Debug.LogError("Bozuk tarih formatý: " + savedDate);
            return;
        }

        DateTime today = DateTime.Now.Date;
        double daysDiff = (today - lastWinDate).TotalDays;

        // Dün kazanýlmadýysa streak bozulur
        if (daysDiff >= 2)
        {
            currentStreak = 0;
            PlayerPrefs.SetInt(CurrentStreakKey, 0);
            PlayerPrefs.Save();
        }
    }

    // Kazanýldýðýnda çaðýr
    public void UpdateStreak()
    {
        DateTime today = DateTime.Now.Date;
        string todayStr = today.ToString(DateFormat);

        string lastWinDateStr = PlayerPrefs.GetString(LastWinDateKey, "");

        // Bugün zaten kazanýldýysa tekrar artýrma
        if (lastWinDateStr == todayStr) return;

        currentStreak++;
        bestStreak = Mathf.Max(bestStreak, currentStreak);

        PlayerPrefs.SetInt(CurrentStreakKey, currentStreak);
        PlayerPrefs.SetInt(BestStreakKey, bestStreak);
        PlayerPrefs.SetString(LastWinDateKey, todayStr);
        PlayerPrefs.Save();

        Debug.Log("Yeni Streak: " + currentStreak);
    }
}