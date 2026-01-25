namespace QuizApplication.Models.Game
{
    /// <summary>
    /// Status sesji gry
    /// </summary>
    public enum GameStatus
    {
        /// <summary>Lobby - oczekiwanie na graczy</summary>
        Lobby,

        /// <summary>Gra w trakcie - wyświetlanie pytań</summary>
        InProgress,

        /// <summary>Wyświetlanie wyników rundy</summary>
        ShowingResults,

        /// <summary>Gra zakończona - ranking końcowy</summary>
        Finished,

        /// <summary>Gra anulowana przez hosta</summary>
        Cancelled
    }
}
