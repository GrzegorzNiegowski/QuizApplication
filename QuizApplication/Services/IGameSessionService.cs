using QuizApplication.DTOs;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.RealTimeDtos;
using QuizApplication.DTOs.SessionDtos;
using QuizApplication.Utilities;
using System.Drawing;

namespace QuizApplication.Services
{
    // Zarządzanie sesją
    public interface IGameSessionService : IDisposable
    {
        OperationResult InitializeSession(StartSessionDto dto, GameQuizDto gameQuiz, string hostUserId);
        bool SessionExists(string sessionCode);
        Task<OperationResult> StartGameAutoAsync(string sessionCode, string hostConnectionId);

        // Zarządzanie graczami
        JoinSessionResultDto AddPlayer(JoinSessionDto dto, string connectionId);
        void RemovePlayer(string connectionId);
        List<PlayerScoreDto> GetPlayersInSession(string sessionCode);
        bool UpdatePlayerConnection(string sessionCode, Guid participantId, string newConnectionId);

        // Helpery
        string? GetSessionIdByConnectionId(string connectionId);
        bool IsHost(string connectionId);
        void SetHostConnectionId(string sessionCode, string connectionId);
        bool IsNicknameTaken(string sessionCode, string nickname);
        bool IsGameInProgress(string sessionCode);
        bool IsPlayerInSessionByNickname(string sessionCode, string nickname);

        // Rozgrywka (Logika) - indywidualna dla każdego gracza
        QuestionForPlayerDto? GetNextQuestionForPlayer(string sessionCode, string connectionId);
        bool IsPlayerFinished(string sessionCode, string connectionId);
        ScoreboardDto GetLeaderboard(string sessionCode);
        bool IsHostOfSession(string sessionCode, string connectionId);
        OperationResult SubmitAnswer(string sessionCode, string connectionId, SubmitAnswerDto dto);
        bool IsPlayerInSession(string sessionCode, string connectionId);

        // Statystyki (dla admin/monitoring)
        SessionStatisticsDto GetSessionStatistics();

    }
}
