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
        /// <summary>Czy pytanie jest wielokrotnego wyboru</summary>
        public bool IsMultipleChoice { get; set; }
        /// <summary>Ile poprawnych odpowiedzi ma pytanie (dla podpowiedzi)</summary>
        public int CorrectAnswersCount { get; set; }
        public List<GameAnswerDto> Answers { get; set; } = new();
    }
}
