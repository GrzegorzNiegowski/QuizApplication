namespace QuizApplication.DTOs.GameDtos
{
    // === Ranking końcowy ===

    /// <summary>
    /// Końcowy ranking
    /// </summary>
    public class FinalRankingDto
    {
        public string QuizTitle { get; set; } = string.Empty;
        public int TotalQuestions { get; set; }
        public List<FinalRankingEntryDto> Rankings { get; set; } = new();
    }
}
