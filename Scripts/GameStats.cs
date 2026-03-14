[System.Serializable]
public class GameStats
{
    public int totalWins;
    public int totalLosses;
    public int[] guessDistribution = new int[7]; // 1. tahminde, 2. tahminde bilme sayýlarý vb.
    public float winRate;
}