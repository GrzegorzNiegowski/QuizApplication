using QuizApplication.DTOs.GameDtos;
using QuizApplication.Models;
namespace QuizApplication.Utilities
{
    public class GameSession
    {
        // Klasa wewnętrzna trzymająca stan jednej gry
        public int QuizId {  get; set; }
        public string? HostUserId { get; set; } // ASP.NET Identity UserId
        public string HostConnectionId { get; set; } = string.Empty; // ID połączenia Hosta
        public List<Player> Players { get; set; } = new();
        // Tu w przyszłości dojdzie np. CurrentQuestionIndex
        public GameQuizDto? QuizData { get; set; }
        public bool IsGameStarted { get; set; } = false;
        public int CurrentQuestionIndex { get; set; } = 0;
        public HashSet<string> AnsweredConnectionIds { get; set; } = new();
        public DateTimeOffset? CurrentQuestionStartUtc { get; set; }
        public int CurrentQuestionTimeLimitSeconds { get; set; }
        public CancellationTokenSource? QuestionCts { get; set; }
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;
        // Lock objects dla różnych operacji
        public object LockObj { get; } = new object();
        private readonly object _answersLock = new object();

        /// <summary>
        /// Thread-safe dodawanie odpowiedzi gracza
        /// </summary>
        public bool TryRecordAnswer(string connectionId)
        {
            lock (_answersLock)
            {
                return AnsweredConnectionIds.Add(connectionId);
            }
        }

        /// <summary>
        /// Aktualizuje czas ostatniej aktywności
        /// </summary>
        public void UpdateActivity()
        {
            LastActivity = DateTimeOffset.UtcNow;
        }
    }
}
