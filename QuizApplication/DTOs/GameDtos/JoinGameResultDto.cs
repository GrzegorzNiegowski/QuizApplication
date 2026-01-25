namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Odpowiedź po dołączeniu do gry
    /// </summary>
    public class JoinGameResultDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
        public Guid? SessionId { get; set; }
        public Guid? PlayerId { get; set; }
        public string? QuizTitle { get; set; }
        public int TotalQuestions { get; set; }
    }

}
