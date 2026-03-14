using UnityEngine;
using TMPro;

public class DiamondCalculation : MonoBehaviour
{
    private int numDiamond;
    public TMP_Text numDiamondIndText;
    public static bool notEnougDiamond = false;

    void Start()
    {
       // PlayerPrefs.SetInt("diamondRemained", 1000);

        numDiamond = PlayerPrefs.GetInt("diamondRemained", 0);

        // PlayerPrefs.SetInt("diamondRemained", 1000);


        if (numDiamondIndText != null)
        {
            numDiamondIndText.text = numDiamond.ToString();
        }


    }

    public void AddDiamond(int toAdd)
    {
        int resultDiamond = numDiamond + toAdd;
        numDiamondIndText.text = resultDiamond.ToString();

        PlayerPrefs.SetInt("diamondRemained", resultDiamond);
        PlayerPrefs.Save();
        numDiamondIndText.text = resultDiamond.ToString();
    }

    public void SpendDiamond(int toSubstract)
    {
        notEnougDiamond = false;

        if (numDiamond - toSubstract < 0)
        {
            Debug.Log("Not Enough Diamonds.");
            notEnougDiamond = true;
            // maðaza paneli aç
            return;
        }

        int resultDiamond = numDiamond - toSubstract;
        numDiamondIndText.text = resultDiamond.ToString();

        PlayerPrefs.SetInt("diamondRemained", resultDiamond);
        PlayerPrefs.Save();
        numDiamondIndText.text = resultDiamond.ToString();
    }

    
}
