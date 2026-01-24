using QuizApplication.DTOs.QuestionDtos;

namespace QuizApplication.DTOs.QuizDtos
{
    /// <summary>
    /// DTO ze szczegółami quizu (do widoku Details)
    /// </summary>
    public class QuizDetailsDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AccessCode { get; set; } = string.Empty;
        public string? OwnerId { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
        public List<QuestionDto> Questions { get; set; } = new();
    }
}
