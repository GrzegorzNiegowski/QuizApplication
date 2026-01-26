namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Odpowiedź gracza
    /// </summary>
    public class SubmitAnswerDto
    {
        /// <summary>Lista wybranych odpowiedzi (dla kompatybilności wstecznej można przekazać jedną)</summary>
        public List<int> AnswerIds { get; set; } = new();
        public double ResponseTimeSeconds { get; set; }
    }

}
