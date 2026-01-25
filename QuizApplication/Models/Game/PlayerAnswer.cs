namespace QuizApplication.Models.Game
{
    /// <summary>
    /// Odpowiedź gracza na pytanie
    /// </summary>
    public class PlayerAnswer
    {
        /// <summary>ID pytania</summary>
        public int QuestionId { get; set; }

        /// <summary>ID wybranej odpowiedzi (null = brak odpowiedzi/timeout)</summary>
        public int? SelectedAnswerId { get; set; }

        /// <summary>Czas odpowiedzi w sekundach od wyświetlenia pytania</summary>
        public double ResponseTimeSeconds { get; set; }

        /// <summary>Czy odpowiedź była poprawna</summary>
        public bool IsCorrect { get; set; }

        /// <summary>Przyznane punkty (0 jeśli błędna lub brak odpowiedzi)</summary>
        public int PointsAwarded { get; set; }

        /// <summary>Czas udzielenia odpowiedzi</summary>
        public DateTime AnsweredAt { get; set; }
    }
}

