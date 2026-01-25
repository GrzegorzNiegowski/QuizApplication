using System.Collections.Concurrent;
using QuizApplication.Models.Game;

namespace QuizApplication.Models
{
    /// <summary>
    /// Gracz w sesji gry
    /// </summary>
    public class Player
    {
        /// <summary>Unikalny identyfikator gracza</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>ID połączenia SignalR</summary>
        public string ConnectionId { get; set; } = string.Empty;

        /// <summary>Nick gracza (wyświetlany w rankingu)</summary>
        public string Nickname { get; set; } = string.Empty;

        /// <summary>ID użytkownika (null = gracz niezalogowany)</summary>
        public string? UserId { get; set; }

        /// <summary>Suma punktów</summary>
        public int TotalScore { get; set; }

        /// <summary>Status połączenia</summary>
        public PlayerStatus Status { get; set; } = PlayerStatus.Connected;

        /// <summary>Czas dołączenia do sesji</summary>
        public DateTime JoinedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Czas ostatniego rozłączenia (dla reconnect)</summary>
        public DateTime? DisconnectedAt { get; set; }

        /// <summary>Odpowiedzi gracza (thread-safe)</summary>
        public ConcurrentDictionary<int, PlayerAnswer> Answers { get; set; } = new();

        /// <summary>Sprawdza czy gracz już odpowiedział na pytanie</summary>
        public bool HasAnswered(int questionId) => Answers.ContainsKey(questionId);

        /// <summary>Dodaje odpowiedź gracza</summary>
        public bool AddAnswer(PlayerAnswer answer)
        {
            return Answers.TryAdd(answer.QuestionId, answer);
        }

        /// <summary>Pobiera odpowiedź na pytanie</summary>
        public PlayerAnswer? GetAnswer(int questionId)
        {
            Answers.TryGetValue(questionId, out var answer);
            return answer;
        }
    }
}
