using QuizApplication.Utilities;

namespace QuizApplication.DTOs.GameDtos
{
    // === Wyniki rundy ===

    /// <summary>
    /// Wyniki rundy wysyłane do wszystkich
    /// </summary>
    public class RoundResultsDto
    {
        public int QuestionId { get; set; }
        public int CorrectAnswerId { get; set; }
        public int NextQuestionInSeconds { get; set; } = GameConstants.ResultsDisplaySeconds;
    }
}
