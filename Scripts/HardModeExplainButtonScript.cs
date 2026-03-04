using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class HardModeExplainButtonScript : MonoBehaviour
{
    // Start is called once before the first execution of Update after the MonoBehaviour is created

    public GameObject hardModeExplainPanel;
    public TMP_Text totalPointsText;
    public TMP_Text currentStreakText;
    // private int currentStreak;

    private void Start()
    {
        int totalPoints = PlayerPrefs.GetInt("Total Points",0);
        int currentStreak = PlayerPrefs.GetInt("CurrentStreak", 0); 
        totalPointsText.text = "Toplam Puaným: " + totalPoints;
        currentStreakText.text = "Mevcut Serim: " + currentStreak;
    }
    public void QuestionMarkButton()
    {
        hardModeExplainPanel.SetActive(true);
    }

    public void ExitHardModeExplainPanel()
    {
        hardModeExplainPanel.SetActive(false);

    }


}
