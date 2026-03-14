using System;
using System.Globalization;
using TMPro;
using UnityEngine;
using UnityEngine.InputSystem.LowLevel;

public class StreakCalculation : MonoBehaviour
{
    public static int currentStreak;
    public static int bestStreak;
    private int totalPoints;
    private int remainedPoints;
    private string gameState = "Daily Mode";
    public TMP_Text currentStreakText, currentPointsText, motivationText;

    private const string CurrentStreakKey = "CurrentStreak";
    private const string BestStreakKey = "BestStreak";
    private const string LastWinDateKey = "LastWinDate_Global";
    public DiamondCalculation diamondCalculation;
    private const string DateFormat = "yyyyMMdd";

    void Awake()
    {
        LoadStats();
        CheckStreakValidity();
        totalPoints = PlayerPrefs.GetInt("Total Points", 0);

    }

    private void Start()
    {
        if (currentStreakText != null)
        {
            currentStreakText.text = "Mevcut Serim: " + currentStreak;
        }
        if (currentPointsText != null)
        {
            currentPointsText.text = "Mevcut Puaným: " + PlayerPrefs.GetInt("Total Points", 0);
        }
        if (motivationText == null) return;
        
        if (totalPoints < SceneLoader.minUnlimitedPoint)
        {
            gameState = "Süresiz Modun";
            remainedPoints = SceneLoader.minUnlimitedPoint - totalPoints;
            motivationText.text = gameState + " kilidini açmana sadece " + remainedPoints.ToString() + " puan kaldý.";

        }
        else if( totalPoints >= SceneLoader.minUnlimitedPoint && totalPoints < SceneLoader.minTimeLimitPoint)
        {
            gameState = "Süreli Modun";
            remainedPoints = SceneLoader.minTimeLimitPoint - totalPoints;
            motivationText.text = gameState + " kilidini açmana sadece " + remainedPoints.ToString() + " puan kaldý.";
        }
        else if (totalPoints >= SceneLoader.minTimeLimitPoint)
        {
            gameState = "Tüm modlarýn kilidini açtýn.";
            motivationText.text = gameState;

        }

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
        if (lastWinDateStr == todayStr)
        {
            diamondCalculation.AddDiamond(1);
            return;
        }
            

        currentStreak++;
        bestStreak = Mathf.Max(bestStreak, currentStreak);

        PlayerPrefs.SetInt(CurrentStreakKey, currentStreak);
        PlayerPrefs.SetInt(BestStreakKey, bestStreak);
        PlayerPrefs.SetString(LastWinDateKey, todayStr);
        PlayerPrefs.Save();

        Debug.Log("Yeni Streak: " + currentStreak);
    }
}