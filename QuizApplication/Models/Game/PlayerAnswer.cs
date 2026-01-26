namespace QuizApplication.Models.Game
{
    /// <summary>
    /// Odpowiedź gracza na pytanie
    /// </summary>
    public class PlayerAnswer
    {
        /// <summary>ID pytania</summary>
        public int QuestionId { get; set; }

        /// <summary>Lista wybranych odpowiedzi (obsługuje multi-choice)</summary>
        public List<int> SelectedAnswerIds { get; set; } = new();

        /// <summary>Czas odpowiedzi w sekundach od wyświetlenia pytania</summary>
        public double ResponseTimeSeconds { get; set; }

        /// <summary>Czy odpowiedź była poprawna (wszystkie poprawne i żadna błędna)</summary>
        public bool IsCorrect { get; set; }

        /// <summary>Przyznane punkty (0 jeśli błędna lub brak odpowiedzi)</summary>
        public int PointsAwarded { get; set; }

        /// <summary>Czas udzielenia odpowiedzi</summary>
        public DateTime AnsweredAt { get; set; }

        /// <summary>Czy gracz odpowiedział (wybrał przynajmniej jedną odpowiedź)</summary>
        public bool HasAnswered => SelectedAnswerIds.Any();
    }
}
