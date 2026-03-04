using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class SceneLoader : MonoBehaviour
{
    public static string clickedButton1Tag;
    public static string clickedButton2Tag;
    public LifeCalcullationScript lifeCalculationScript;
    public MainSceneButtonManager mainSceneButtonManager;

    private int totalPoints;
    private const int minUnlimitedPoint = 500;
    private const int minTimeLimitPoint = 1000;
    private int isUnlockedUnlimted;
    private int isUnlockedTimelimit;

    public static bool HardMode =>
        PlayerPrefs.GetInt("HardMode", 0) == 1;

    private void Start()
    {
        isUnlockedUnlimted = PlayerPrefs.GetInt("Unlock Unlimited",0);
        isUnlockedTimelimit = PlayerPrefs.GetInt("Unlock Timelimit", 0);
        totalPoints = PlayerPrefs.GetInt("Total Points", 0);
        lifeCalculationScript = Object.FindFirstObjectByType<LifeCalcullationScript>();
    }
    public void LoadScene(string sceneName)
    {
        ButtonClicked();

        if (!string.IsNullOrEmpty(sceneName))
            SceneManager.LoadScene(sceneName);
    }

    public void LoadScene2()
    {
        ButtonClicked();

        // int points = PlayerPrefs.GetInt("Total Points");

        if (!lifeCalculationScript.CheckLife() && lifeCalculationScript != null)
        {
            Debug.Log("Yetersiz Can");

            
            return;
        }

        if (clickedButton1Tag == "Unlimited Game Button" && totalPoints < minUnlimitedPoint)
        {
            Debug.Log("Not enough points to unlock the Unlimited Mode");
            mainSceneButtonManager.LockedUnlimitedPanel();
            return;
        }

        if (clickedButton1Tag == "TimeLimit Game Button" && totalPoints < minTimeLimitPoint)
        {
            Debug.Log("Not enough points to unlock the TimeLimit Mode");
            mainSceneButtonManager.LockedTimeLimitPanel();

            return;
        }



        if (clickedButton1Tag == "Unlimited Game Button" && WordLengthSelector.selectedLength == 4)
        {
            if (isUnlockedUnlimted == 0)
            {
                PlayerPrefs.SetInt("Unlock Unlimited", 1);
                //PlayerPrefs.SetInt("Total Points", totalPoints - minUnlimitedPoint);

            }
            SceneManager.LoadScene("4 Letter Unlimited Game Scene");
        }

        else if (clickedButton1Tag == "TimeLimit Game Button" && WordLengthSelector.selectedLength == 4)
        {
            if (isUnlockedTimelimit == 0)
            {
                PlayerPrefs.SetInt("Unlock Timelimit", 1);
                //PlayerPrefs.SetInt("Total Points", totalPoints - minTimeLimitPoint);

            }
            SceneManager.LoadScene("4 Letter TimeLimit Game Scene");
        }
            

        else if (clickedButton1Tag == "Daily Game Button" && WordLengthSelector.selectedLength == 4)
            SceneManager.LoadScene("4 Letter DailyWord Game Scene");

        else if (clickedButton1Tag == "Unlimited Game Button" && WordLengthSelector.selectedLength == 5)
        {
            if (isUnlockedUnlimted == 0)
            {
                PlayerPrefs.SetInt("Unlock Unlimited", 1);
                //PlayerPrefs.SetInt("Total Points", totalPoints - minUnlimitedPoint);
            }
       
            SceneManager.LoadScene("5 Letter Unlimited Game Scene");
        }

        else if (clickedButton1Tag == "TimeLimit Game Button" && WordLengthSelector.selectedLength == 5)
        {
            if (isUnlockedTimelimit == 0)
            {
                PlayerPrefs.SetInt("Unlock Timelimit", 1);
                //PlayerPrefs.SetInt("Total Points", totalPoints - minTimeLimitPoint);

            }
            SceneManager.LoadScene("5 Letter TimeLimit Game Scene");

        }

        else if (clickedButton1Tag == "Daily Game Button" && WordLengthSelector.selectedLength == 5)
            SceneManager.LoadScene("5 Letter DailyWord Game Scene");

        else if (clickedButton1Tag == "Unlimited Game Button" && WordLengthSelector.selectedLength == 6)
        {
            if (isUnlockedUnlimted == 0)
            {
                PlayerPrefs.SetInt("Unlock Unlimited", 1);
                //PlayerPrefs.SetInt("Total Points", totalPoints - minUnlimitedPoint);

            }
            SceneManager.LoadScene("6 Letter Unlimited Game Scene");

        }

        else if (clickedButton1Tag == "TimeLimit Game Button" && WordLengthSelector.selectedLength == 6)
        {
            if (isUnlockedTimelimit == 0)
            {
                PlayerPrefs.SetInt("Unlock Timelimit", 1);
                //PlayerPrefs.SetInt("Total Points", totalPoints - minUnlimitedPoint);
            }
            SceneManager.LoadScene("6 Letter TimeLimit Game Scene");

        }
            

        else if (clickedButton1Tag == "Daily Game Button" && WordLengthSelector.selectedLength == 6)
            SceneManager.LoadScene("6 Letter DailyWord Game Scene");
    }

    void ButtonClicked()
    {
        var clicked = EventSystem.current.currentSelectedGameObject;
        if (clicked == null) return;

        if (SceneManager.GetActiveScene().name == "Main Scene")
            clickedButton1Tag = clicked.tag;

        else if (SceneManager.GetActiveScene().name == "Word Length Selection Scene")
            clickedButton2Tag = clicked.tag;
    }

    public void RestartScene()
    {
        Scene activeScene = SceneManager.GetActiveScene();
        SceneManager.LoadScene(activeScene.name);
    }

}
