public interface IGameManager
{
    bool Win { get; }

    bool GameEnded { get; }
    void AddLetter(string letter);
    void DeleteLetter();
    void SubmitGuess();
    void ClearRow();
}
