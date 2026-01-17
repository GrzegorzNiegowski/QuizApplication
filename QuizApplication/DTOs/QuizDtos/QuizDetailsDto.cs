namespace QuizApplication.DTOs.QuizDtos
{
    public class QuizDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AccessCode { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; }
        public List<QuestionSummaryDto> Questions { get; set; } = new();
    }
}
