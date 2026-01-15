namespace QuizApplication.DTOs.QuizDtos
{
    public class QuizSummaryDto // Do listy quizów (lżejszy obiekt)
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AccessCode { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
