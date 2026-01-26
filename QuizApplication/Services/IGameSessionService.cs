using QuizApplication.Models;
using QuizApplication.Models.Game;

namespace QuizApplication.Services
{
    /// <summary>
    /// Serwis zarządzający sesjami gry (in-memory)
    /// </summary>
    public interface IGameSessionService
    {
        // === Zarządzanie sesją ===

        /// <summary>Tworzy nową sesję gry</summary>
        Task<GameSession> CreateSessionAsync(int quizId, string quizTitle, string accessCode,
            string hostConnectionId, string hostUserId, List<int> questionIds);

        /// <summary>Pobiera sesję po ID</summary>
        GameSession? GetSession(Guid sessionId);

        /// <summary>Pobiera sesję po kodzie dostępu</summary>
        GameSession? GetSessionByAccessCode(string accessCode);

        /// <summary>Usuwa sesję</summary>
        bool RemoveSession(Guid sessionId);

        /// <summary>Pobiera wszystkie aktywne sesje</summary>
        IEnumerable<GameSession> GetAllActiveSessions();

        // === Zarządzanie graczami ===

        /// <summary>Dodaje gracza do sesji (lub reconnect jeśli gracz istnieje)</summary>
        /// <returns>Success, ErrorMessage, Player, IsReconnect</returns>
        Task<(bool Success, string? ErrorMessage, Player? Player, bool IsReconnect)> JoinSessionAsync(
            string accessCode, string nickname, string connectionId, string? userId = null);

        /// <summary>Usuwa gracza z sesji</summary>
        Task<bool> LeaveSessionAsync(Guid sessionId, Guid playerId);

        /// <summary>Oznacza gracza jako rozłączonego</summary>
        Task<bool> DisconnectPlayerAsync(string connectionId);

        /// <summary>Reconnect gracza (po rozłączeniu)</summary>
        Task<(bool Success, GameSession? Session, Player? Player)> ReconnectPlayerAsync(
            string accessCode, string nickname, string newConnectionId);

        /// <summary>Znajduje sesję gracza po ConnectionId</summary>
        (GameSession? Session, Player? Player) FindPlayerByConnectionId(string connectionId);

        // === Kontrola gry ===

        /// <summary>Rozpoczyna grę (tylko host)</summary>
        Task<(bool Success, string? ErrorMessage)> StartGameAsync(Guid sessionId, string hostConnectionId);

        /// <summary>Przechodzi do następnego pytania</summary>
        Task<(bool Success, int? QuestionId)> NextQuestionAsync(Guid sessionId);

        /// <summary>Kończy wyświetlanie pytania (timeout lub wszyscy odpowiedzieli)</summary>
        Task<bool> EndQuestionAsync(Guid sessionId);

        /// <summary>Kończy grę</summary>
        Task<bool> FinishGameAsync(Guid sessionId);

        /// <summary>Anuluje grę (host)</summary>
        Task<bool> CancelGameAsync(Guid sessionId, string hostConnectionId);

        // === Odpowiedzi ===

        /// <summary>Zapisuje odpowiedź gracza (obsługuje multi-choice)</summary>
        Task<(bool Success, int PointsAwarded, string? ErrorMessage)> SubmitAnswerAsync(
            Guid sessionId, Guid playerId, List<int> answerIds, double responseTimeSeconds);

        /// <summary>Pobiera wyniki rundy</summary>
        RoundResults GetRoundResults(Guid sessionId, int questionId);

        /// <summary>Pobiera końcowy ranking</summary>
        List<PlayerRankingEntry> GetFinalRanking(Guid sessionId);

        // === Punktacja ===

        /// <summary>Oblicza punkty za odpowiedź</summary>
        int CalculatePoints(int maxPoints, double responseTimeSeconds, double totalTimeSeconds);

        // === Czyszczenie ===

        /// <summary>Usuwa nieaktywne sesje</summary>
        Task CleanupInactiveSessionsAsync();
    }

    /// <summary>
    /// Wyniki rundy (dla jednego pytania)
    /// </summary>
    public class RoundResults
    {
        public int QuestionId { get; set; }
        public int CorrectAnswerId { get; set; }
        public List<PlayerRoundResult> PlayerResults { get; set; } = new();
        public List<PlayerRankingEntry> TopPlayers { get; set; } = new();
    }

    /// <summary>
    /// Wynik gracza w rundzie
    /// </summary>
    public class PlayerRoundResult
    {
        public Guid PlayerId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public List<int> SelectedAnswerIds { get; set; } = new();
        public bool IsCorrect { get; set; }
        public int PointsAwarded { get; set; }
        public double ResponseTimeSeconds { get; set; }
        public int TotalScore { get; set; }
        public int Rank { get; set; }
    }

    /// <summary>
    /// Wpis w rankingu
    /// </summary>
    public class PlayerRankingEntry
    {
        public int Rank { get; set; }
        public Guid PlayerId { get; set; }
        public string Nickname { get; set; } = string.Empty;
        public int TotalScore { get; set; }
        public int CorrectAnswers { get; set; }
        public double AverageResponseTime { get; set; }
    }
}
