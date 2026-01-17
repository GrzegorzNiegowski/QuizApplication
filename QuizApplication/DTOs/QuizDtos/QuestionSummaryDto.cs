using QuizApplication.DTOs.QuestionDtos;
namespace QuizApplication.DTOs.QuizDtos
{
    public class QuestionSummaryDto // Skrót pytania na liście wewnątrz quizu
    {
        public int QuestionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public int Points { get; set; }

        public string? ImageUrl { get; set; }
        public List<AnswerSummaryDto> Answers { get; set; } = new();
    }
}
