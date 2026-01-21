using QuizApplication.DTOs.GameDtos;
using QuizApplication.Models;
namespace QuizApplication.Utilities
{
    public class GameSession
    {
        // Klasa wewnętrzna trzymająca stan jednej gry
        public int QuizId { get; set; }
        public string? HostUserId { get; set; } // ASP.NET Identity UserId
        public string HostConnectionId { get; set; } = string.Empty; // ID połączenia Hosta
        public List<Player> Players { get; set; } = new();
        public GameQuizDto? QuizData { get; set; }
        public bool IsGameStarted { get; set; } = false;
        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;
        public DateTimeOffset LastActivity { get; set; } = DateTimeOffset.UtcNow;

        // CancellationToken do zakończenia gry
        public CancellationTokenSource? GameCts { get; set; }

        // Lock objects dla różnych operacji
        public object LockObj { get; } = new object();

        /// <summary>
        /// Aktualizuje czas ostatniej aktywności
        /// </summary>
        public void UpdateActivity()
        {
            LastActivity = DateTimeOffset.UtcNow;
        }
    }

    /// <summary>
    /// Stan indywidualnej gry gracza
    /// </summary>
    public class PlayerGameState
    {
        public int CurrentQuestionIndex { get; set; } = -1;
        public HashSet<int> AnsweredQuestionIds { get; set; } = new();
        public DateTimeOffset? CurrentQuestionStartUtc { get; set; }
        public bool IsFinished { get; set; } = false;
    }
}
