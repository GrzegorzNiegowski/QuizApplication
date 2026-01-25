namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Wpis w końcowym rankingu
    /// </summary>
    public class FinalRankingEntryDto
    {
        public int Rank { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int CorrectAnswers { get; set; }
        public double AverageResponseTime { get; set; }
        public bool IsCurrentPlayer { get; set; }
    }
}
