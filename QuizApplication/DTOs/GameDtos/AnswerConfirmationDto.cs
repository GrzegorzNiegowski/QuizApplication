namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Potwierdzenie odpowiedzi
    /// </summary>
    public class AnswerConfirmationDto
    {
        public bool Success { get; set; }
        public string? ErrorMessage { get; set; }
    }
}
