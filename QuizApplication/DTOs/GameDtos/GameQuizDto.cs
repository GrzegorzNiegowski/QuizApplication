namespace QuizApplication.DTOs.GameDtos
{
    public class GameQuizDto
    {
        public int Id { get; set; }
        public string AccessCode { get; set; } = string.Empty;
        public string Title { get; set; } = string.Empty;
        public List<GameQuestionDto> Questions { get; set; } = new();
    }
}
