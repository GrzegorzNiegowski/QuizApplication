namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Gracz w lobby
    /// </summary>
    public class LobbyPlayerDto
    {
        public Guid PlayerId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public bool IsConnected { get; set; }
    }
}
