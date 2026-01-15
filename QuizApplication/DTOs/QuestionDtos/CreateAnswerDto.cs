namespace QuizApplication.DTOs.QuestionDtos
{
    public class CreateAnswerDto
    {
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
