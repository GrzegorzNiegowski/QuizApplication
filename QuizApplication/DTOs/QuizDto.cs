namespace QuizApplication.DTOs
{
    public class QuizDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AccessCode { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
        public DateTimeOffset CreatedAt { get; set; }

        // Opcjonalnie liczba pytań (przydatne na liście)
        public int QuestionCount { get; set; }

        // Lista pytań (tylko w szczegółach)
        public List<QuestionDto> Questions { get; set; } = new();
    }
}
