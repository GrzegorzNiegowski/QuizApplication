namespace QuizApplication.DTOs.RealTimeDtos
{
    public class ScoreboardDto
    {
        public List<PlayerScoreDto> Players { get; set; } = new();
    }
}
