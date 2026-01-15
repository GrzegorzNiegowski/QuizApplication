namespace QuizApplication.DTOs.GameDtos
{
    public class GameAnswerDto
    {
        public int Id { get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } // Tu jest flaga, której potrzebuje serwer!
    }
}
