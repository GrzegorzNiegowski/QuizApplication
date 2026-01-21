namespace QuizApplication.DTOs.QuestionDtos
{
    public class CreateQuestionDto
    {
        public int QuizId { get; set; }

        public string? ImageUrl { get; set; }
        public string Content { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public int Points { get; set; }
        public List<CreateAnswerDto> Answers { get; set; } = new();

    }
}
