using UnityEngine;
using UnityEngine.UI;

public class SoundToggleManager : MonoBehaviour
{
    public Toggle musicToggle;
    public RawImage musicIcon;

    void Start()
    {
        // Kayýtlý deðeri oku
        bool musicOn = PlayerPrefs.GetInt("MusicOn", 1) == 1;

        // Toggle’ý doðru pozisyona getir
        musicToggle.isOn = musicOn;
        musicIcon.gameObject.SetActive(musicOn);
    }

    // Toggle OnValueChanged'e baðlanacak
    public void OnMusicToggleChanged(bool isOn)
    {
        // Kaydet
        PlayerPrefs.SetInt("MusicOn", isOn ? 1 : 0);
        PlayerPrefs.Save();

        // Müziði kontrol et
        if (isOn)
        {
            MusicManager.Instance.PlayMusic();
            musicIcon.gameObject.SetActive(true);


        }
        else
        {
            MusicManager.Instance.StopMusic();
            musicIcon.gameObject.SetActive(false);
        }
    }
}
