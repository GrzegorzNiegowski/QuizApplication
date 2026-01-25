using QuizApplication.DTOs.GameDtos;
using QuizApplication.Models;
using System.Collections.Concurrent;
namespace QuizApplication.Models.Game
{
    /// <summary>
    /// Sesja gry quizowej
    /// </summary>
    public class GameSession
    {
        /// <summary>Unikalny identyfikator sesji</summary>
        public Guid Id { get; set; } = Guid.NewGuid();

        /// <summary>Kod dostępu (z Quiz.AccessCode)</summary>
        public string AccessCode { get; set; } = string.Empty;

        /// <summary>ID quizu</summary>
        public int QuizId { get; set; }

        /// <summary>Tytuł quizu (cache)</summary>
        public string QuizTitle { get; set; } = string.Empty;

        /// <summary>ID połączenia SignalR hosta</summary>
        public string HostConnectionId { get; set; } = string.Empty;

        /// <summary>ID użytkownika hosta</summary>
        public string HostUserId { get; set; } = string.Empty;

        /// <summary>Status gry</summary>
        public GameStatus Status { get; set; } = GameStatus.Lobby;

        /// <summary>Indeks aktualnego pytania (0-based)</summary>
        public int CurrentQuestionIndex { get; set; } = -1;

        /// <summary>Czas rozpoczęcia aktualnego pytania</summary>
        public DateTime? QuestionStartedAt { get; set; }

        /// <summary>Czas utworzenia sesji</summary>
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        /// <summary>Całkowita liczba pytań w quizie</summary>
        public int TotalQuestions { get; set; }

        /// <summary>Lista ID pytań (kolejność)</summary>
        public List<int> QuestionIds { get; set; } = new();

        /// <summary>Gracze w sesji (thread-safe)</summary>
        public ConcurrentDictionary<Guid, Player> Players { get; set; } = new();

        /// <summary>Obiekt do synchronizacji</summary>
        private readonly object _lock = new();

        /// <summary>Liczba aktywnych graczy</summary>
        public int ActivePlayerCount => Players.Values.Count(p => p.Status == PlayerStatus.Connected);

        /// <summary>Czy sesja jest aktywna (można dołączyć)</summary>
        public bool CanJoin => Status == GameStatus.Lobby;

        /// <summary>Czy gra jest w trakcie</summary>
        public bool IsInProgress => Status == GameStatus.InProgress || Status == GameStatus.ShowingResults;

        /// <summary>Aktualne pytanie ID (null jeśli gra nie rozpoczęta)</summary>
        public int? CurrentQuestionId =>
            CurrentQuestionIndex >= 0 && CurrentQuestionIndex < QuestionIds.Count
                ? QuestionIds[CurrentQuestionIndex]
                : null;

        /// <summary>Czy jest następne pytanie</summary>
        public bool HasNextQuestion => CurrentQuestionIndex + 1 < TotalQuestions;

        /// <summary>Dodaje gracza do sesji</summary>
        public bool TryAddPlayer(Player player)
        {
            if (!CanJoin) return false;
            return Players.TryAdd(player.Id, player);
        }

        /// <summary>Usuwa gracza z sesji</summary>
        public bool TryRemovePlayer(Guid playerId)
        {
            return Players.TryRemove(playerId, out _);
        }

        /// <summary>Znajduje gracza po ConnectionId</summary>
        public Player? GetPlayerByConnectionId(string connectionId)
        {
            return Players.Values.FirstOrDefault(p => p.ConnectionId == connectionId);
        }

        /// <summary>Znajduje gracza po nicku</summary>
        public Player? GetPlayerByNickname(string nickname)
        {
            return Players.Values.FirstOrDefault(p =>
                p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Sprawdza czy nick jest zajęty</summary>
        public bool IsNicknameTaken(string nickname)
        {
            return Players.Values.Any(p =>
                p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
        }

        /// <summary>Liczba graczy którzy odpowiedzieli na aktualne pytanie</summary>
        public int GetAnsweredCount()
        {
            if (!CurrentQuestionId.HasValue) return 0;
            return Players.Values.Count(p =>
                p.Status == PlayerStatus.Connected && p.HasAnswered(CurrentQuestionId.Value));
        }

        /// <summary>Czy wszyscy aktywni gracze odpowiedzieli</summary>
        public bool AllPlayersAnswered()
        {
            if (!CurrentQuestionId.HasValue) return false;
            var activePlayers = Players.Values.Where(p => p.Status == PlayerStatus.Connected).ToList();
            return activePlayers.Count > 0 && activePlayers.All(p => p.HasAnswered(CurrentQuestionId.Value));
        }

        /// <summary>Pobiera ranking graczy</summary>
        public List<Player> GetRanking()
        {
            return Players.Values
                .OrderByDescending(p => p.TotalScore)
                .ThenBy(p => p.JoinedAt)
                .ToList();
        }

        /// <summary>Nazwa grupy SignalR dla tej sesji</summary>
        public string GroupName => $"game_{Id}";
    }
}
