using UnityEngine;
using System.IO;

public class SaveManager : MonoBehaviour
{
    private string filePath;

    void Awake()
    {
        filePath = Application.persistentDataPath + "/stats.json";
    }

    // Veriyi Kaydet
    public void SaveStats(GameStats stats)
    {
        string json = JsonUtility.ToJson(stats, true);
        File.WriteAllText(filePath, json);
    }

    // Veriyi Yükle
    public GameStats LoadStats()
    {
        if (!File.Exists(filePath)) return new GameStats(); // Dosya yoksa yeni bir tane oluþtur

        string json = File.ReadAllText(filePath);
        return JsonUtility.FromJson<GameStats>(json);
    }
}