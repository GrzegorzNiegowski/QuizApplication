namespace QuizApplication.DTOs.GameDtos
{
    // === Rozgrywka ===

    /// <summary>
    /// Pytanie wysyłane do graczy
    /// </summary>
    public class GameQuestionDto
    {
        public int QuestionId { get; set; }
        public int QuestionNumber { get; set; }
        public int TotalQuestions { get; set; }
        public string Content { get; set; } = string.Empty;
        public string? ImageUrl { get; set; }
        public int TimeLimitSeconds { get; set; }
        public int MaxPoints { get; set; }
        public List<GameAnswerDto> Answers { get; set; } = new();
    }
}
