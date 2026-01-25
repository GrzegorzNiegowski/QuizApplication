namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Wynik gracza w rundzie (wysyłany indywidualnie)
    /// </summary>
    public class PlayerRoundResultDto
    {
        public bool IsCorrect { get; set; }
        public int PointsAwarded { get; set; }
        public int TotalScore { get; set; }
        public int CurrentRank { get; set; }
        public double ResponseTimeSeconds { get; set; }
    }
}
