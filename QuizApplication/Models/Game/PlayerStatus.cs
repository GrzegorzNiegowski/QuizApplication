namespace QuizApplication.Models.Game
{
    /// <summary>
    /// Status połączenia gracza
    /// </summary>
    public enum PlayerStatus
    {
        /// <summary>Gracz połączony</summary>
        Connected,

        /// <summary>Gracz rozłączony (może wrócić)</summary>
        Disconnected
    }
}
