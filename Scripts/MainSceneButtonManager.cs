using UnityEngine;
using UnityEngine.UI;

public class MainSceneButtonManager : MonoBehaviour
{
    public Button enoughLifeButton;
    public GameObject unlimitedLocked;
    public GameObject timelimitLocked;
   // public GameObject statsPanel;


    public void EnoughLife()
    {
        int remainedLife = PlayerPrefs.GetInt("lifeRemained");

        if (remainedLife <= 0)
        {
            enoughLifeButton.gameObject.SetActive(true);
        }
        else
        {
            enoughLifeButton.interactable = false;
        }
    }

    public void LockedUnlimitedPanel()
    {
        unlimitedLocked.SetActive(true);
    }

    public void LockedUnlimitedPanelExit()
    {
        unlimitedLocked.SetActive(false);
    }

    public void LockedTimeLimitPanel()
    {
        timelimitLocked.SetActive(true);
    }

    public void LockedTimeLimitPanelExit()
    {
        timelimitLocked.SetActive(false);
    }
}
