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
        /// <summary>Lista poprawnych odpowiedzi (obsługuje multi-choice)</summary>
        public List<int> CorrectAnswerIds { get; set; } = new();
        public int NextQuestionInSeconds { get; set; } = GameConstants.ResultsDisplaySeconds;
    }
}
