namespace QuizApplication.DTOs.RealTimeDtos
{
    public class QuestionForPlayerDto
    {
        public int QuestionId { get; set; }
        public string Content { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public int Points { get; set; }
        public int CurrentQuestionIndex { get; set; } // np. Pytanie 1 z 10
        public int TotalQuestions { get; set; }
        public List<AnswerForPlayerDto> Answers { get; set; } = new();

        public DateTimeOffset ServerStartUtc { get; set; }
    }
}
