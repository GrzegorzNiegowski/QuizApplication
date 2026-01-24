namespace QuizApplication.DTOs.QuizDtos
{
    /// <summary>
    /// DTO do listy quizów (wersja skrócona)
    /// </summary>
    public class QuizListDto
    {
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string AccessCode { get; set; } = string.Empty;
        public int QuestionCount { get; set; }
        public DateTimeOffset CreatedAt { get; set; }
    }
}
