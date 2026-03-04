using UnityEngine;
using UnityEngine.UI;
public class HowToPlayPanelExitButtonScript : MonoBehaviour
{
    public Image panel;
    public Button exitButton;

    public void PanelExitButtonManager()
    {
        panel.gameObject.SetActive(false);
    }

    public void HowToPlayButton()
    {
        panel.gameObject.SetActive(true);
    }

}
