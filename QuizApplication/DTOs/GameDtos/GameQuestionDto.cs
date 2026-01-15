namespace QuizApplication.DTOs.GameDtos
{
    public class GameQuestionDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public int TimeLimitSeconds { get; set; }
        public int Points { get; set; }
        public List<GameAnswerDto> Answers { get; set; } = new();
    }
}
