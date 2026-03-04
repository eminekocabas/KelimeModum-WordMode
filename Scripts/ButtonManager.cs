
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class ButtonManager : MonoBehaviour
{
    public GameObject gameOverScene;
    public GameObject congratsScene;
    public Button seeMyPointsButton;
    public Button giveUpButton;
    public static bool giveUp = false;

    public MonoBehaviour gameScript;
    private IGameManager gameResult;


    void Start()
    {
        gameResult = gameScript as IGameManager;
        Button[] allButtonsInScene = Resources.FindObjectsOfTypeAll<Button>();

        foreach (Button btn in allButtonsInScene)
        {
            if (btn.name == "Give Up Button")
            {
                giveUpButton = btn;
                giveUpButton.gameObject.SetActive(SceneLoader.HardMode);
                giveUpButton.onClick.AddListener(GiveUp);
                Debug.Log("buton bulundu");
            }
            else if (btn.name == "See My Points Button")
            {
                seeMyPointsButton = btn;
                seeMyPointsButton.onClick.AddListener(SeeMyPointsButton);
                Debug.Log("buton bulundu");
            }
        }

       // if (gameResult == null)
           // Debug.LogError("GameScript IGameResult implement etmiyor!")
    }

    public void GiveUp()
    {

        giveUp = true;
      //  Debug.Log("You Gave Up...");

    }


    public void SeeStatsCongratsButton() 
    { 
        congratsScene.SetActive(false);
        seeMyPointsButton.gameObject.SetActive(true);
    }

    public void SeeStatsGameOverButton()
    {
        gameOverScene.SetActive(false);
        seeMyPointsButton.gameObject.SetActive(true);
    }


    public void SeeMyPointsButton()
    {
        if (gameResult == null) return;

        if (gameResult.Win)
        {
            congratsScene.SetActive(true);
        }
        else
        {
            gameOverScene.SetActive(true);
        }
    }

    public void GameOvePanelExit()
    {
        gameOverScene.SetActive(false);
    }

    public void CongratsPanelExit()
    {
        congratsScene.SetActive(false);
    }



}
