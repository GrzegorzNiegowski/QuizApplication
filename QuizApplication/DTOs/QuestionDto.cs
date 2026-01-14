namespace QuizApplication.DTOs
{
    public class QuestionDto
    {
        public int? Id { get; set; }
        public int QuizId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int TimeLimitSeconds {  get; set; }
        public int Points { get; set; }
        public string? ImageUrl { get; set; }

        public List<AnswerDto> Answers { get; set; } = new();
    }
}
