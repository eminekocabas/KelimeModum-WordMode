using UnityEngine;

public static class StatsService
{
    // Veriyi bellekte tutuyoruz ki her seferinde dosyadan okumayalým (Performans)
    private static GameStats _currentStats;
    private static string _filePath = Application.persistentDataPath + "/stats.json";

    public static GameStats Data
    {
        get
        {
            if (_currentStats == null) _currentStats = Load();
            return _currentStats;
        }
    }

    public static void Save()
    {
        string json = JsonUtility.ToJson(_currentStats, true);
        System.IO.File.WriteAllText(_filePath, json);
    }

    private static GameStats Load()
    {
        if (!System.IO.File.Exists(_filePath)) return new GameStats();
        return JsonUtility.FromJson<GameStats>(System.IO.File.ReadAllText(_filePath));
    }


}