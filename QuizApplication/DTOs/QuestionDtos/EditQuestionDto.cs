namespace QuizApplication.DTOs.QuestionDtos
{
    public class EditQuestionDto
    {
        public int QuestionId { get; set; }
        public int QuizId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public int Points { get; set; }
        public List<EditAnswerDto> Answers { get; set; } = new();
    }
}
