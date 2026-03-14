using UnityEngine;
using TMPro;
using UnityEngine.UI;

public class EditUserNameInput : MonoBehaviour
{
    public TMP_InputField inputField;
    public Button saveButton;
    public Button editButton;

    void Start()
    {
        // Kayýtlý isim varsa yükle
        if (PlayerPrefs.HasKey("UserName"))
            inputField.text = PlayerPrefs.GetString("UserName");

        // Baþta kilitli
        inputField.interactable = false;
    }

    public void EnableEdit()
    {
        inputField.interactable = true;
        inputField.ActivateInputField(); // cursor aç
        saveButton.gameObject.SetActive(true);
        editButton.gameObject.SetActive(false);
    }

    public void SaveAndLock()
    {
        PlayerPrefs.SetString("UserName", inputField.text);
        PlayerPrefs.Save();

        inputField.interactable = false;
        saveButton.gameObject.SetActive(false);
        editButton.gameObject.SetActive(true);
    }
}