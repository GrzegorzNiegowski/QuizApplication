namespace QuizApplication.DTOs.GameDtos
{
    // === Lobby ===

    /// <summary>
    /// Informacje o sesji dla lobby
    /// </summary>
    public class LobbyInfoDto
    {
        public Guid SessionId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
        public string AccessCode { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public List<LobbyPlayerDto> Players { get; set; } = new();
    }
}
