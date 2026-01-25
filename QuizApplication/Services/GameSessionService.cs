using Microsoft.Extensions.Logging;
using QuizApplication.Models;
using QuizApplication.Models.Game;
using QuizApplication.Utilities;
using System.Collections.Concurrent;

namespace QuizApplication.Services
{
    /// <summary>
    /// Implementacja serwisu sesji gry (in-memory, singleton)
    /// </summary>
    public class GameSessionService : IGameSessionService
    {
        private readonly ConcurrentDictionary<Guid, GameSession> _sessions = new();
        private readonly ConcurrentDictionary<string, Guid> _accessCodeToSessionId = new();
        private readonly ILogger<GameSessionService> _logger;

        public GameSessionService(ILogger<GameSessionService> logger)
        {
            _logger = logger;
        }

        #region Session Management

        public Task<GameSession> CreateSessionAsync(int quizId, string quizTitle, string accessCode,
            string hostConnectionId, string hostUserId, List<int> questionIds)
        {
            // Sprawdź czy nie ma już aktywnej sesji dla tego kodu
            if (_accessCodeToSessionId.ContainsKey(accessCode))
            {
                var existingSession = GetSessionByAccessCode(accessCode);
                if (existingSession != null && existingSession.IsInProgress)
                {
                    throw new InvalidOperationException("Sesja dla tego quizu już istnieje");
                }
                // Usuń starą nieaktywną sesję
                RemoveSession(existingSession!.Id);
            }

            var session = new GameSession
            {
                QuizId = quizId,
                QuizTitle = quizTitle,
                AccessCode = accessCode,
                HostConnectionId = hostConnectionId,
                HostUserId = hostUserId,
                QuestionIds = questionIds,
                TotalQuestions = questionIds.Count
            };

            if (_sessions.TryAdd(session.Id, session))
            {
                _accessCodeToSessionId.TryAdd(accessCode, session.Id);
                _logger.LogInformation("Utworzono sesję {SessionId} dla quizu {QuizId} (kod: {AccessCode})",
                    session.Id, quizId, accessCode);
            }

            return Task.FromResult(session);
        }

        public GameSession? GetSession(Guid sessionId)
        {
            _sessions.TryGetValue(sessionId, out var session);
            return session;
        }

        public GameSession? GetSessionByAccessCode(string accessCode)
        {
            if (_accessCodeToSessionId.TryGetValue(accessCode.ToUpperInvariant(), out var sessionId))
            {
                return GetSession(sessionId);
            }
            return null;
        }

        public bool RemoveSession(Guid sessionId)
        {
            if (_sessions.TryRemove(sessionId, out var session))
            {
                _accessCodeToSessionId.TryRemove(session.AccessCode, out _);
                _logger.LogInformation("Usunięto sesję {SessionId}", sessionId);
                return true;
            }
            return false;
        }

        public IEnumerable<GameSession> GetAllActiveSessions()
        {
            return _sessions.Values.Where(s =>
                s.Status != GameStatus.Finished && s.Status != GameStatus.Cancelled);
        }

        #endregion

        #region Player Management

        public Task<(bool Success, string? ErrorMessage, Player? Player)> JoinSessionAsync(
            string accessCode, string nickname, string connectionId, string? userId = null)
        {
            var session = GetSessionByAccessCode(accessCode);

            if (session == null)
            {
                return Task.FromResult<(bool, string?, Player?)>((false, GameConstants.Messages.GameNotFound, null));
            }

            if (!session.CanJoin)
            {
                return Task.FromResult<(bool, string?, Player?)>((false, GameConstants.Messages.GameAlreadyStarted, null));
            }

            if (session.Players.Count >= GameConstants.MaxPlayersPerSession)
            {
                return Task.FromResult<(bool, string?, Player?)>((false, GameConstants.Messages.GameFull, null));
            }

            if (session.IsNicknameTaken(nickname))
            {
                return Task.FromResult<(bool, string?, Player?)>((false, GameConstants.Messages.NicknameTaken, null));
            }

            var player = new Player
            {
                Nickname = nickname,
                ConnectionId = connectionId,
                UserId = userId
            };

            if (session.TryAddPlayer(player))
            {
                _logger.LogInformation("Gracz {Nickname} dołączył do sesji {SessionId}", nickname, session.Id);
                return Task.FromResult<(bool, string?, Player?)>((true, null, player));
            }

            return Task.FromResult<(bool, string?, Player?)>((false, "Nie udało się dołączyć do gry", null));
        }

        public Task<bool> LeaveSessionAsync(Guid sessionId, Guid playerId)
        {
            var session = GetSession(sessionId);
            if (session == null) return Task.FromResult(false);

            if (session.TryRemovePlayer(playerId))
            {
                _logger.LogInformation("Gracz {PlayerId} opuścił sesję {SessionId}", playerId, sessionId);
                return Task.FromResult(true);
            }
            return Task.FromResult(false);
        }

        public Task<bool> DisconnectPlayerAsync(string connectionId)
        {
            var (session, player) = FindPlayerByConnectionId(connectionId);
            if (session == null || player == null) return Task.FromResult(false);

            player.Status = PlayerStatus.Disconnected;
            player.DisconnectedAt = DateTime.UtcNow;

            _logger.LogInformation("Gracz {Nickname} rozłączony w sesji {SessionId}",
                player.Nickname, session.Id);

            return Task.FromResult(true);
        }

        public Task<(bool Success, GameSession? Session, Player? Player)> ReconnectPlayerAsync(
            string accessCode, string nickname, string newConnectionId)
        {
            var session = GetSessionByAccessCode(accessCode);
            if (session == null)
            {
                return Task.FromResult<(bool, GameSession?, Player?)>((false, null, null));
            }

            var player = session.GetPlayerByNickname(nickname);
            if (player == null || player.Status != PlayerStatus.Disconnected)
            {
                return Task.FromResult<(bool, GameSession?, Player?)>((false, null, null));
            }

            // Sprawdź timeout reconnectu
            if (player.DisconnectedAt.HasValue)
            {
                var disconnectedFor = DateTime.UtcNow - player.DisconnectedAt.Value;
                if (disconnectedFor.TotalSeconds > GameConstants.PlayerReconnectTimeoutSeconds)
                {
                    session.TryRemovePlayer(player.Id);
                    return Task.FromResult<(bool, GameSession?, Player?)>((false, null, null));
                }
            }

            player.ConnectionId = newConnectionId;
            player.Status = PlayerStatus.Connected;
            player.DisconnectedAt = null;

            _logger.LogInformation("Gracz {Nickname} ponownie połączony w sesji {SessionId}",
                nickname, session.Id);

            return Task.FromResult<(bool, GameSession?, Player?)>((true, session, player));
        }

        public (GameSession? Session, Player? Player) FindPlayerByConnectionId(string connectionId)
        {
            foreach (var session in _sessions.Values)
            {
                var player = session.GetPlayerByConnectionId(connectionId);
                if (player != null)
                {
                    return (session, player);
                }
            }
            return (null, null);
        }

        #endregion

        #region Game Control

        public Task<(bool Success, string? ErrorMessage)> StartGameAsync(Guid sessionId, string hostConnectionId)
        {
            var session = GetSession(sessionId);
            if (session == null)
            {
                return Task.FromResult<(bool, string?)>((false, GameConstants.Messages.GameNotFound));
            }

            if (session.HostConnectionId != hostConnectionId)
            {
                return Task.FromResult<(bool, string?)>((false, GameConstants.Messages.NotAuthorized));
            }

            if (session.ActivePlayerCount < GameConstants.MinPlayersToStart)
            {
                return Task.FromResult<(bool, string?)>((false, GameConstants.Messages.NotEnoughPlayers));
            }

            session.Status = GameStatus.InProgress;
            _logger.LogInformation("Gra rozpoczęta w sesji {SessionId}", sessionId);

            return Task.FromResult<(bool, string?)>((true, null));
        }

        public Task<(bool Success, int? QuestionId)> NextQuestionAsync(Guid sessionId)
        {
            var session = GetSession(sessionId);
            if (session == null || session.Status != GameStatus.InProgress)
            {
                return Task.FromResult<(bool, int?)>((false, null));
            }

            session.CurrentQuestionIndex++;

            if (session.CurrentQuestionIndex >= session.TotalQuestions)
            {
                return Task.FromResult<(bool, int?)>((false, null));
            }

            session.QuestionStartedAt = DateTime.UtcNow;
            var questionId = session.QuestionIds[session.CurrentQuestionIndex];

            _logger.LogDebug("Pytanie {Index}/{Total} w sesji {SessionId}",
                session.CurrentQuestionIndex + 1, session.TotalQuestions, sessionId);

            return Task.FromResult<(bool, int?)>((true, questionId));
        }

        public Task<bool> EndQuestionAsync(Guid sessionId)
        {
            var session = GetSession(sessionId);
            if (session == null) return Task.FromResult(false);

            session.Status = GameStatus.ShowingResults;
            return Task.FromResult(true);
        }

        public Task<bool> FinishGameAsync(Guid sessionId)
        {
            var session = GetSession(sessionId);
            if (session == null) return Task.FromResult(false);

            session.Status = GameStatus.Finished;
            _logger.LogInformation("Gra zakończona w sesji {SessionId}", sessionId);

            return Task.FromResult(true);
        }

        public Task<bool> CancelGameAsync(Guid sessionId, string hostConnectionId)
        {
            var session = GetSession(sessionId);
            if (session == null) return Task.FromResult(false);

            if (session.HostConnectionId != hostConnectionId)
            {
                return Task.FromResult(false);
            }

            session.Status = GameStatus.Cancelled;
            _logger.LogInformation("Gra anulowana w sesji {SessionId}", sessionId);

            return Task.FromResult(true);
        }

        #endregion

        #region Answers

        public Task<(bool Success, int PointsAwarded, string? ErrorMessage)> SubmitAnswerAsync(
            Guid sessionId, Guid playerId, int answerId, double responseTimeSeconds)
        {
            var session = GetSession(sessionId);
            if (session == null || session.Status != GameStatus.InProgress)
            {
                return Task.FromResult<(bool, int, string?)>((false, 0, GameConstants.Messages.GameNotFound));
            }

            if (!session.CurrentQuestionId.HasValue)
            {
                return Task.FromResult<(bool, int, string?)>((false, 0, "Brak aktywnego pytania"));
            }

            if (!session.Players.TryGetValue(playerId, out var player))
            {
                return Task.FromResult<(bool, int, string?)>((false, 0, "Gracz nie znaleziony"));
            }

            var questionId = session.CurrentQuestionId.Value;

            if (player.HasAnswered(questionId))
            {
                return Task.FromResult<(bool, int, string?)>((false, 0, GameConstants.Messages.AlreadyAnswered));
            }

            // Punkty będą obliczone później gdy znamy poprawną odpowiedź
            var answer = new PlayerAnswer
            {
                QuestionId = questionId,
                SelectedAnswerId = answerId,
                ResponseTimeSeconds = responseTimeSeconds,
                AnsweredAt = DateTime.UtcNow
            };

            if (player.AddAnswer(answer))
            {
                _logger.LogDebug("Gracz {Nickname} odpowiedział na pytanie {QuestionId} w {Time}s",
                    player.Nickname, questionId, responseTimeSeconds);
                return Task.FromResult<(bool, int, string?)>((true, 0, null)); // Punkty obliczane w GetRoundResults
            }

            return Task.FromResult<(bool, int, string?)>((false, 0, "Nie udało się zapisać odpowiedzi"));
        }

        public RoundResults GetRoundResults(Guid sessionId, int questionId)
        {
            var session = GetSession(sessionId);
            if (session == null)
            {
                return new RoundResults();
            }

            var results = new RoundResults
            {
                QuestionId = questionId
            };

            // Pobierz ranking na potrzeby wyników
            var ranking = session.GetRanking();

            for (int i = 0; i < ranking.Count; i++)
            {
                var player = ranking[i];
                var answer = player.GetAnswer(questionId);

                var playerResult = new PlayerRoundResult
                {
                    PlayerId = player.Id,
                    Nickname = player.Nickname,
                    SelectedAnswerId = answer?.SelectedAnswerId,
                    IsCorrect = answer?.IsCorrect ?? false,
                    PointsAwarded = answer?.PointsAwarded ?? 0,
                    ResponseTimeSeconds = answer?.ResponseTimeSeconds ?? 0,
                    TotalScore = player.TotalScore,
                    Rank = i + 1
                };

                results.PlayerResults.Add(playerResult);
            }

            // Top 5 graczy
            results.TopPlayers = ranking.Take(5).Select((p, i) => new PlayerRankingEntry
            {
                Rank = i + 1,
                PlayerId = p.Id,
                Nickname = p.Nickname,
                TotalScore = p.TotalScore,
                CorrectAnswers = p.Answers.Values.Count(a => a.IsCorrect)
            }).ToList();

            return results;
        }

        public List<PlayerRankingEntry> GetFinalRanking(Guid sessionId)
        {
            var session = GetSession(sessionId);
            if (session == null) return new List<PlayerRankingEntry>();

            var ranking = session.GetRanking();

            return ranking.Select((player, index) =>
            {
                var answers = player.Answers.Values.ToList();
                var correctAnswers = answers.Count(a => a.IsCorrect);
                var avgTime = answers.Any() ? answers.Average(a => a.ResponseTimeSeconds) : 0;

                return new PlayerRankingEntry
                {
                    Rank = index + 1,
                    PlayerId = player.Id,
                    Nickname = player.Nickname,
                    TotalScore = player.TotalScore,
                    CorrectAnswers = correctAnswers,
                    AverageResponseTime = Math.Round(avgTime, 2)
                };
            }).ToList();
        }

        #endregion

        #region Scoring

        public int CalculatePoints(int maxPoints, double responseTimeSeconds, double totalTimeSeconds)
        {
            if (responseTimeSeconds <= 0) responseTimeSeconds = 0.1;
            if (responseTimeSeconds >= totalTimeSeconds) return GameConstants.MinPointsForCorrectAnswer;

            var remainingTime = totalTimeSeconds - responseTimeSeconds;
            var ratio = remainingTime / totalTimeSeconds;
            var points = (int)(maxPoints * ratio);

            return Math.Max(points, GameConstants.MinPointsForCorrectAnswer);
        }

        #endregion

        #region Cleanup

        public Task CleanupInactiveSessionsAsync()
        {
            var cutoff = DateTime.UtcNow.AddMinutes(-GameConstants.SessionLobbyTimeoutMinutes);
            var toRemove = _sessions.Values
                .Where(s => s.Status == GameStatus.Lobby && s.CreatedAt < cutoff)
                .Select(s => s.Id)
                .ToList();

            foreach (var id in toRemove)
            {
                RemoveSession(id);
                _logger.LogInformation("Usunięto nieaktywną sesję {SessionId}", id);
            }

            // Usuń też zakończone sesje starsze niż 1 godzina
            var finishedCutoff = DateTime.UtcNow.AddHours(-1);
            var finishedToRemove = _sessions.Values
                .Where(s => (s.Status == GameStatus.Finished || s.Status == GameStatus.Cancelled)
                            && s.CreatedAt < finishedCutoff)
                .Select(s => s.Id)
                .ToList();

            foreach (var id in finishedToRemove)
            {
                RemoveSession(id);
            }

            return Task.CompletedTask;
        }

        #endregion
    }
}
