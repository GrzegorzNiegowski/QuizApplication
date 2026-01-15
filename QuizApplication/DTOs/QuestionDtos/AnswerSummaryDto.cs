namespace QuizApplication.DTOs.QuestionDtos
{
    public class AnswerSummaryDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
