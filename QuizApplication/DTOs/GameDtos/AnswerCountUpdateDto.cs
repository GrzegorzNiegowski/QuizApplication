namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Aktualizacja liczby odpowiedzi (dla hosta)
    /// </summary>
    public class AnswerCountUpdateDto
    {
        public int AnsweredCount { get; set; }
        public int TotalPlayers { get; set; }
    }
}
