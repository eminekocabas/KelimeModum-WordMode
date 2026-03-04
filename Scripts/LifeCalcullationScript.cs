using UnityEngine;
using TMPro;
using System;


public class LifeCalcullationScript : MonoBehaviour
{
    public static int lifeRemained;
    public TMP_Text lifeRemainedText;
    public TMP_Text timerText;

    private const int maxLife = 5;
    private const float refillTime = 1800f; // saniye

    private DateTime lastLifeTime;

    void Awake()
    {
        lifeRemained = PlayerPrefs.GetInt("lifeRemained", maxLife);
        Transform lifeTransform = transform.Find("Text (TMP)");
        Transform timerTransform = transform.Find("CountDown Text");

        if (lifeTransform != null)
            lifeRemainedText = lifeTransform.GetComponent<TMP_Text>();

        if (timerTransform != null)
            timerText = timerTransform.GetComponent<TMP_Text>();


        if (PlayerPrefs.HasKey("lastLifeTime"))
        {
            lastLifeTime = DateTime.Parse(PlayerPrefs.GetString("lastLifeTime"));

            if (lifeRemained < maxLife)
            {
                TimeSpan timePassed = DateTime.Now - lastLifeTime;
                int livesToAdd = (int)(timePassed.TotalSeconds / refillTime);

                if (livesToAdd > 0)
                {
                    lifeRemained = Mathf.Clamp(lifeRemained + livesToAdd, 0, maxLife);

                    double remainderSeconds = timePassed.TotalSeconds % refillTime;
                    lastLifeTime = DateTime.Now.AddSeconds(-remainderSeconds);

                    PlayerPrefs.SetInt("lifeRemained", lifeRemained);
                    if (lifeRemained >= maxLife)
                        PlayerPrefs.DeleteKey("lastLifeTime");
                    else
                        PlayerPrefs.SetString("lastLifeTime", lastLifeTime.ToString());

                    PlayerPrefs.Save();
                }
            }
        }

        lifeRemainedText.text = lifeRemained.ToString();
    }

    void Update()
    {
        if (timerText == null)
        {
            return;
        }
        if (lifeRemained >= maxLife)
        {
           timerText.text = "FULL";
            return;
        }

        TimeSpan timePassed = DateTime.Now - lastLifeTime;
        float remainingTime = refillTime - (float)timePassed.TotalSeconds;

        if (remainingTime <= 0)
        {
            AddLife();
        }
        else
        {
            int minutes = Mathf.FloorToInt(remainingTime / 60f);
            int seconds = Mathf.FloorToInt(remainingTime % 60f);

            timerText.text = $"{minutes:00}:{seconds:00}";
        }
    }

    void AddLife()
    {
        lifeRemained++;
        lifeRemainedText.text = lifeRemained.ToString();

        PlayerPrefs.SetInt("lifeRemained", lifeRemained);

        if (lifeRemained < maxLife)
        {
            lastLifeTime = DateTime.Now;
            PlayerPrefs.SetString("lastLifeTime", lastLifeTime.ToString());
        }
        else
        {
            PlayerPrefs.DeleteKey("lastLifeTime");
            timerText.text = "FULL";
        }

        PlayerPrefs.Save();
    }

    // OYUN BÝTTÝÐÝNDE ÇAÐIR
    public void DecreaseLife()
    {
        if (lifeRemained <= 0)
            return;

        lifeRemained--;
        lifeRemainedText.text = lifeRemained.ToString();

        PlayerPrefs.SetInt("lifeRemained", lifeRemained);

        // Eðer timer zaten çalýþmýyorsa baþlat
        if (!PlayerPrefs.HasKey("lastLifeTime"))
        {
            lastLifeTime = DateTime.Now;
            PlayerPrefs.SetString("lastLifeTime", lastLifeTime.ToString());
        }

        PlayerPrefs.Save();
    }

    public bool CheckLife()
    {
        if (PlayerPrefs.GetInt("lifeRemained") == 0)
        {
            return false;
        }
        else
            { return true; }
    }
}