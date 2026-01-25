namespace QuizApplication.DTOs.GameDtos
{
    public class TopPlayerEntryDto
    {
        public int Rank { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int PointsThisRound { get; set; }
    }
}
