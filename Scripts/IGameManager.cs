public interface IGameManager
{
    bool Win { get; }
    void AddLetter(string letter);
    void DeleteLetter();
    void SubmitGuess();
    void ClearRow();
}
