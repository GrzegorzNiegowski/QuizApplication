namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Odpowiedź gracza
    /// </summary>
    public class SubmitAnswerDto
    {
        public int AnswerId { get; set; }
        public double ResponseTimeSeconds { get; set; }
    }

}
