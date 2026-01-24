using Microsoft.AspNetCore.SignalR;
using QuizApplication.DTOs;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.RealTimeDtos;
using QuizApplication.DTOs.SessionDtos;
//using QuizApplication.Hubs;
using QuizApplication.Models;
using QuizApplication.Utilities;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace QuizApplication.Services
{
    public class GameSessionService : IGameSessionService
    {
        private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
        private readonly ConcurrentDictionary<string, string> _connectionToSession = new();
        private readonly IHubContext<QuizHub> _hub;
        private readonly ILogger<GameSessionService> _logger;
        private readonly Timer _cleanupTimer;
        private bool _disposed = false;

        public GameSessionService(IHubContext<QuizHub> hub, ILogger<GameSessionService> logger)
        {
            _hub = hub;
            _logger = logger;

            // Czyszczenie co 10 minut
            _cleanupTimer = new Timer(
                CleanupExpiredSessions,
                null,
                TimeSpan.FromMinutes(GameConstants.SessionCleanupIntervalMinutes),
                TimeSpan.FromMinutes(GameConstants.SessionCleanupIntervalMinutes)
            );

            _logger.LogInformation("GameSessionService initialized with cleanup interval: {Interval} minutes",
                GameConstants.SessionCleanupIntervalMinutes);
        }

        //#region Session Management

        public OperationResult InitializeSession(StartSessionDto dto, GameQuizDto gameQuiz, string hostUserId)
        {
            var code = SessionCodeHelper.Normalize(gameQuiz.AccessCode);

            if (!SessionCodeHelper.IsValid(code))
            {
                return OperationResult.Fail("Nieprawidłowy kod sesji");
            }

            if (_sessions.TryGetValue(code, out var existing))
            {
                // Jeśli to ten sam host i gra nie wystartowała - pozwól
                if (existing.HostUserId == hostUserId && !existing.IsGameStarted)
                {
                    _logger.LogInformation("Host {HostId} reconnecting to session {Code}", hostUserId, code);
                    existing.UpdateActivity();
                    return OperationResult.Ok();
                }

                // Jeśli gra trwa
                if (existing.IsGameStarted)
                {
                    return OperationResult.Fail("Gra już trwa dla tego quizu");
                }

                // Inny host
                return OperationResult.Fail("Sesja już istnieje z innym hostem");
            }

            var session = new GameSession
            {
                QuizId = dto.QuizId,
                QuizData = gameQuiz,
                HostUserId = hostUserId,
                Players = new List<Player>(),
                HostConnectionId = "",
                IsGameStarted = false,
                CreatedAt = DateTimeOffset.UtcNow,
                LastActivity = DateTimeOffset.UtcNow
            };

            if (_sessions.TryAdd(code, session))
            {
                _logger.LogInformation("Session {Code} initialized for quiz {QuizId} by host {HostId}",
                    code, dto.QuizId, hostUserId);
                return OperationResult.Ok();
            }

            return OperationResult.Fail("Nie udało się utworzyć sesji");
        }

        public bool SessionExists(string sessionCode)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);
            return !string.IsNullOrEmpty(code) && _sessions.ContainsKey(code);
        }


        //#endregion
        //#region Player Management

        public JoinSessionResultDto AddPlayer(JoinSessionDto dto, string connectionId)
        {
            var code = SessionCodeHelper.Normalize(dto.SessionCode);

            if (!_sessions.TryGetValue(code, out var session))
            {
                return new JoinSessionResultDto { Success = false, Error = "Sesja nie istnieje" };
            }

            // Blokuj dołączanie podczas trwającej gry
            if (session.IsGameStarted)
            {
                return new JoinSessionResultDto { Success = false, Error = "Gra już trwa, nie można dołączyć" };
            }

            lock (session.Players)
            {
                // Sprawdź limit graczy
                if (session.Players.Count >= GameConstants.MaxPlayersPerSession)
                {
                    return new JoinSessionResultDto
                    {
                        Success = false,
                        Error = $"Osiągnięto maksymalną liczbę graczy ({GameConstants.MaxPlayersPerSession})"
                    };
                }

                // Sprawdź czy nick jest zajęty
                if (session.Players.Any(p => p.Nickname.Equals(dto.PlayerName, StringComparison.OrdinalIgnoreCase)))
                {
                    return new JoinSessionResultDto { Success = false, Error = "Nick zajęty" };
                }

                var participantId = dto.ParticipantId ?? Guid.NewGuid();

                var player = new Player
                {
                    ParticipantId = participantId,
                    ConnectionId = connectionId,
                    Nickname = dto.PlayerName,
                    Score = 0
                };

                session.Players.Add(player);
                _connectionToSession[connectionId] = code;
                session.UpdateActivity();

                _logger.LogInformation("Player {Nick} ({ParticipantId}) joined session {Code}",
                    dto.PlayerName, participantId, code);

                return new JoinSessionResultDto
                {
                    Success = true,
                    SessionCode = code,
                    ParticipantId = participantId
                };
            }
        }

        public void RemovePlayer(string connectionId)
        {
            var code = GetSessionIdByConnectionId(connectionId);
            if (code != null && _sessions.TryGetValue(code, out var session))
            {
                lock (session.Players)
                {
                    var player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
                    if (player != null)
                    {
                        session.Players.Remove(player);
                        _connectionToSession.TryRemove(connectionId, out _);
                        session.UpdateActivity();

                        _logger.LogInformation("Player {Nick} removed from session {Code}",
                            player.Nickname, code);
                    }
                }
            }
        }

        public bool UpdatePlayerConnection(string sessionCode, Guid participantId, string newConnectionId)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (!_sessions.TryGetValue(code, out var session))
            {
                return false;
            }

            lock (session.Players)
            {
                var player = session.Players.FirstOrDefault(p => p.ParticipantId == participantId);
                if (player == null)
                {
                    return false;
                }

                // Usuń stare mapowanie
                _connectionToSession.TryRemove(player.ConnectionId, out _);

                // Ustaw nowe
                player.ConnectionId = newConnectionId;
                _connectionToSession[newConnectionId] = code;
                session.UpdateActivity();

                _logger.LogInformation("Player {Nick} ({ParticipantId}) reconnected to session {Code}",
                    player.Nickname, participantId, code);

                return true;
            }
        }

        public List<PlayerScoreDto> GetPlayersInSession(string sessionCode)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (_sessions.TryGetValue(code, out var session))
            {
                lock (session.Players)
                {
                    return session.Players
                        .Select(p => new PlayerScoreDto
                        {
                            PlayerName = p.Nickname,
                            Score = p.Score
                        })
                        .ToList();
                }
            }
            return new List<PlayerScoreDto>();
        }

        //#endregion

        //#region Game Flow

        public async Task<OperationResult> StartGameAutoAsync(string sessionCode, string hostConnectionId)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (!_sessions.TryGetValue(code, out var session))
            {
                return OperationResult.Fail("Sesja nie istnieje.");
            }

            // Sprawdź czy to host
            if (session.HostConnectionId != hostConnectionId)
            {
                return OperationResult.Fail("Tylko host może rozpocząć grę.");
            }

            lock (session.LockObj)
            {
                if (session.IsGameStarted)
                {
                    return OperationResult.Fail("Gra już wystartowała.");
                }

                if (session.QuizData == null || session.QuizData.Questions.Count == 0)
                {
                    return OperationResult.Fail("Quiz nie ma żadnych pytań.");
                }

                if (session.Players.Count == 0)
                {
                    return OperationResult.Fail("Brak graczy w sesji.");
                }

                session.IsGameStarted = true;
                session.UpdateActivity();

                // Anuluj poprzedni task jeśli istnieje
                session.GameCts?.Cancel();
                session.GameCts?.Dispose();
                session.GameCts = new CancellationTokenSource();

                // Zresetuj stan gry dla wszystkich graczy
                lock (session.Players)
                {
                    foreach (var player in session.Players)
                    {
                        player.GameState = new PlayerGameState();
                        player.Score = 0;
                    }
                }
            }

            _logger.LogInformation("Game started for session {Code} with {PlayerCount} players",
                code, session.Players.Count);

            // Powiadom wszystkich że gra się rozpoczęła
            await _hub.Clients.Group(code).SendAsync("GameStarted");

            return OperationResult.Ok();
        }

        /// <summary>
        /// Pobiera następne pytanie dla gracza (indywidualna rozgrywka)
        /// </summary>
        public QuestionForPlayerDto? GetNextQuestionForPlayer(string sessionCode, string connectionId)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (!_sessions.TryGetValue(code, out var session))
            {
                return null;
            }

            if (!session.IsGameStarted || session.QuizData == null)
            {
                return null;
            }

            Player? player;
            lock (session.Players)
            {
                player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            }

            if (player == null)
            {
                return null;
            }

            var gameState = player.GameState;

            // Przejdź do następnego pytania
            gameState.CurrentQuestionIndex++;

            // Sprawdź czy są jeszcze pytania
            if (gameState.CurrentQuestionIndex >= session.QuizData.Questions.Count)
            {
                gameState.IsFinished = true;
                return null;
            }

            var question = session.QuizData.Questions[gameState.CurrentQuestionIndex];
            gameState.CurrentQuestionStartUtc = DateTimeOffset.UtcNow;

            session.UpdateActivity();

            return new QuestionForPlayerDto
            {
                QuestionId = question.Id,
                Content = question.Content,
                TimeLimitSeconds = question.TimeLimitSeconds,
                Points = question.Points,
                CurrentQuestionIndex = gameState.CurrentQuestionIndex + 1,
                TotalQuestions = session.QuizData.Questions.Count,
                ServerStartUtc = gameState.CurrentQuestionStartUtc.Value,
                Answers = question.Answers.Select(a => new AnswerForPlayerDto
                {
                    AnswerId = a.Id,
                    Content = a.Content
                }).ToList()
            };
        }

        /// <summary>
        /// Sprawdza czy gracz zakończył quiz
        /// </summary>
        public bool IsPlayerFinished(string sessionCode, string connectionId)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (!_sessions.TryGetValue(code, out var session))
            {
                return true;
            }

            Player? player;
            lock (session.Players)
            {
                player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            }

            return player?.GameState.IsFinished ?? true;
        }

        private object BuildRevealPayload(string code, int questionId)
        {
            if (!_sessions.TryGetValue(code, out var s) || s.QuizData == null)
            {
                return new { QuestionId = questionId, CorrectAnswerId = (int?)null };
            }

            var q = s.QuizData.Questions.FirstOrDefault(x => x.Id == questionId);
            var correct = q?.Answers.FirstOrDefault(a => a.IsCorrect);

            return new
            {
                QuestionId = questionId,
                CorrectAnswerId = correct?.Id
            };
        }

        public OperationResult SubmitAnswer(string sessionCode, string connectionId, SubmitAnswerDto dto)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (!_sessions.TryGetValue(code, out var s))
            {
                return OperationResult.Fail("Sesja nie istnieje.");
            }

            // Czy gra trwa
            if (!s.IsGameStarted)
            {
                return OperationResult.Fail("Gra jeszcze się nie rozpoczęła.");
            }

            if (s.QuizData == null)
            {
                return OperationResult.Fail("Brak danych quizu.");
            }

            // Znajdź gracza
            Player? player;
            lock (s.Players)
            {
                player = s.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
            }

            if (player == null)
            {
                return OperationResult.Fail("Nie jesteś graczem w tej sesji.");
            }

            var gameState = player.GameState;

            // Sprawdź czy gracz nie skończył
            if (gameState.IsFinished)
            {
                return OperationResult.Fail("Quiz został już zakończony.");
            }

            // Sprawdź czy pytanie odpowiada aktualnemu pytaniu gracza
            if (gameState.CurrentQuestionIndex < 0 || gameState.CurrentQuestionIndex >= s.QuizData.Questions.Count)
            {
                return OperationResult.Fail("Brak aktywnego pytania.");
            }

            var currentQ = s.QuizData.Questions[gameState.CurrentQuestionIndex];

            if (currentQ.Id != dto.QuestionId)
            {
                return OperationResult.Fail("To pytanie nie jest aktualne.");
            }

            // Sprawdź czy już odpowiedział na to pytanie
            if (gameState.AnsweredQuestionIds.Contains(dto.QuestionId))
            {
                return OperationResult.Fail("Już odpowiedziałeś na to pytanie.");
            }

            // Sprawdź czas
            var start = gameState.CurrentQuestionStartUtc;
            if (start == null)
            {
                return OperationResult.Fail("Brak aktywnego pytania.");
            }

            var elapsed = DateTimeOffset.UtcNow - start.Value;
            if (elapsed.TotalSeconds > currentQ.TimeLimitSeconds + GameConstants.TimerToleranceSeconds)
            {
                return OperationResult.Fail("Czas na odpowiedź minął.");
            }

            // Zapisz odpowiedź
            gameState.AnsweredQuestionIds.Add(dto.QuestionId);

            // Czy answer należy do pytania
            var answer = currentQ.Answers.FirstOrDefault(a => a.Id == dto.AnswerId);
            if (answer == null)
            {
                return OperationResult.Fail("Nieprawidłowa odpowiedź dla tego pytania.");
            }

            // Dodaj punkty jeśli poprawna
            if (answer.IsCorrect)
            {
                player.Score += currentQ.Points;
                _logger.LogDebug("Player {Nick} scored {Points} points for question {QId}",
                    player.Nickname, currentQ.Points, dto.QuestionId);
            }

            s.UpdateActivity();
            return OperationResult.Ok();
        }

        public ScoreboardDto GetLeaderboard(string sessionCode)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);
            var result = new ScoreboardDto();

            if (_sessions.TryGetValue(code, out var s))
            {
                lock (s.Players)
                {
                    result.Players = s.Players
                        .OrderByDescending(p => p.Score)
                        .Select(p => new PlayerScoreDto
                        {
                            PlayerName = p.Nickname,
                            Score = p.Score
                        })
                        .ToList();
                }
            }

            return result;
        }

        //#endregion

        //#region Helpers

        public string? GetSessionIdByConnectionId(string connectionId)
        {
            return _connectionToSession.TryGetValue(connectionId, out var accessCode) ? accessCode : null;
        }

        public bool IsHost(string connectionId)
        {
            return _sessions.Values.Any(s => s.HostConnectionId == connectionId);
        }

        public void SetHostConnectionId(string sessionCode, string connectionId)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (_sessions.TryGetValue(code, out var session))
            {
                session.HostConnectionId = connectionId;
                _connectionToSession[connectionId] = code;
                session.UpdateActivity();

                _logger.LogInformation("Host connection set for session {Code}", code);
            }
        }

        public bool IsNicknameTaken(string sessionCode, string nickname)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (_sessions.TryGetValue(code, out var s))
            {
                lock (s.Players)
                {
                    return s.Players.Any(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
                }
            }

            return false;
        }

        public bool IsGameInProgress(string sessionCode)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (_sessions.TryGetValue(code, out var s))
            {
                return s.IsGameStarted;
            }

            return false;
        }

        public bool IsPlayerInSessionByNickname(string sessionCode, string nickname)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (_sessions.TryGetValue(code, out var s))
            {
                lock (s.Players)
                {
                    return s.Players.Any(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
                }
            }

            return false;
        }

        public bool IsHostOfSession(string sessionCode, string connectionId)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);
            return _sessions.TryGetValue(code, out var s) && s.HostConnectionId == connectionId;
        }

        public bool IsPlayerInSession(string sessionCode, string connectionId)
        {
            var code = SessionCodeHelper.Normalize(sessionCode);

            if (!_sessions.TryGetValue(code, out var s))
            {
                return false;
            }

            lock (s.Players)
            {
                return s.Players.Any(p => p.ConnectionId == connectionId);
            }
        }

        public SessionStatisticsDto GetSessionStatistics()
        {
            var stats = new SessionStatisticsDto
            {
                TotalSessions = _sessions.Count,
                ActiveGames = _sessions.Values.Count(s => s.IsGameStarted),
                WaitingInLobby = _sessions.Values.Count(s => !s.IsGameStarted),
                TotalPlayers = _sessions.Values.Sum(s => s.Players.Count),
                ActiveSessions = _sessions.Select(kvp => new SessionInfoDto
                {
                    SessionCode = kvp.Key,
                    QuizId = kvp.Value.QuizId,
                    QuizTitle = kvp.Value.QuizData?.Title ?? "Unknown"
                }).ToList()
            };

            return stats;
        }

        //#endregion

        //#region Cleanup

        private void CleanupExpiredSessions(object? state)
        {
            if (_disposed) return;

            try
            {
                var now = DateTimeOffset.UtcNow;
                var toRemove = new List<string>();

                foreach (var kvp in _sessions)
                {
                    var session = kvp.Value;
                    var timeSinceActivity = now - session.LastActivity;

                    // Usuń sesje nieaktywne > 2h
                    if (timeSinceActivity.TotalHours > GameConstants.InactiveSessionTimeoutHours)
                    {
                        toRemove.Add(kvp.Key);
                        _logger.LogInformation("Marking inactive session {Code} for cleanup (inactive for {Hours:F1}h)",
                            kvp.Key, timeSinceActivity.TotalHours);
                        continue;
                    }

                    // Usuń zakończone gry > 30 min (wszystkie gracze skończyli)
                    if (session.IsGameStarted && timeSinceActivity.TotalMinutes > GameConstants.CompletedGameTimeoutMinutes)
                    {
                        bool allFinished;
                        lock (session.Players)
                        {
                            allFinished = session.Players.All(p => p.GameState.IsFinished);
                        }

                        if (allFinished)
                        {
                            toRemove.Add(kvp.Key);
                            _logger.LogInformation("Marking completed game session {Code} for cleanup", kvp.Key);
                        }
                    }
                }

                foreach (var code in toRemove)
                {
                    if (_sessions.TryRemove(code, out var removed))
                    {
                        // Anuluj task gry jeśli jeszcze działa
                        try
                        {
                            removed.GameCts?.Cancel();
                            removed.GameCts?.Dispose();
                        }
                        catch (Exception ex)
                        {
                            _logger.LogWarning(ex, "Error disposing CTS for session {Code}", code);
                        }

                        // Wyczyść connectionId mapping
                        var connectionsToRemove = _connectionToSession
                            .Where(c => c.Value == code)
                            .Select(c => c.Key)
                            .ToList();

                        foreach (var conn in connectionsToRemove)
                        {
                            _connectionToSession.TryRemove(conn, out _);
                        }

                        _logger.LogInformation("Cleaned up session {Code}", code);
                    }
                }

                if (toRemove.Any())
                {
                    _logger.LogInformation("Cleanup completed: removed {Count} sessions", toRemove.Count);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error during session cleanup");
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;

            _logger.LogInformation("Disposing GameSessionService");

            _cleanupTimer?.Dispose();

            // Anuluj wszystkie aktywne gry
            foreach (var session in _sessions.Values)
            {
                try
                {
                    session.GameCts?.Cancel();
                    session.GameCts?.Dispose();
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Error disposing session CTS");
                }
            }
            _sessions.Clear();
            _connectionToSession.Clear();
        }
        //#endregion



       
    }
}

