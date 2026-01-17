using QuizApplication.DTOs;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.RealTimeDtos;
using QuizApplication.DTOs.SessionDtos;
using QuizApplication.Utilities;
using System.Drawing;

namespace QuizApplication.Services
{
    public interface IGameSessionService
    {
        // Zarządzanie sesją
        void InitializeSession(StartSessionDto dto, GameQuizDto gameQuiz);
        bool SessionExists(string sessionCode);

        // Zarządzanie graczami
        JoinSessionResultDto AddPlayer(JoinSessionDto dto, string connectionId);
        void RemovePlayer(string connectionId);
        List<PlayerScoreDto> GetPlayersInSession(string sessionCode);

        // Helpery
        string? GetSessionIdByConnectionId(string connectionId);
        bool IsHost(string connectionId);
        void SetHostConnectionId(string sessionCode, string connectionId);
        bool IsNicknameTaken(string sessionCode, string nickname);

        // Rozgrywka (Logika)
        QuestionForPlayerDto? NextQuestion(string sessionCode);
        ScoreboardDto GetLeaderboard(string sessionCode);
        bool IsHostOfSession(string sessionCode, string connectionId);

        OperationResult SubmitAnswer(string sessionCode, string connectionId, SubmitAnswerDto dto);
        bool IsPlayerInSession(string sessionCode, string connectionId);
    }
}
