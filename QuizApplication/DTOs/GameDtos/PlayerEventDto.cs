namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Zdarzenie gracza (dołączył/wyszedł)
    /// </summary>
    public class PlayerEventDto
    {
        public string EventType { get; set; } = string.Empty; // joined, left, disconnected, reconnected
        public Guid PlayerId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int PlayerCount { get; set; }
    }
}
