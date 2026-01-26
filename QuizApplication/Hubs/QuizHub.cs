using Microsoft.AspNetCore.SignalR;
using QuizApplication.Data;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.Models.Game;
using QuizApplication.Services;
using QuizApplication.Utilities;
using Microsoft.EntityFrameworkCore;
using System.Collections.Concurrent;

namespace QuizApplication.Hubs
{
    /// <summary>
    /// SignalR Hub dla gry quizowej
    /// </summary>
    public class QuizHub : Hub
    {
        private readonly IGameSessionService _gameSessionService;
        private readonly IServiceScopeFactory _scopeFactory;
        private readonly ILogger<QuizHub> _logger;

        // Przechowuje timery dla rozłączonych hostów (do anulowania po timeout)
        private static readonly ConcurrentDictionary<Guid, CancellationTokenSource> _hostDisconnectTimers = new();

        public QuizHub(
            IGameSessionService gameSessionService,
            IServiceScopeFactory scopeFactory,
            ILogger<QuizHub> logger)
        {
            _gameSessionService = gameSessionService;
            _scopeFactory = scopeFactory;
            _logger = logger;
        }

        #region Connection Events

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;

            // Sprawdź czy to gracz
            var (session, player) = _gameSessionService.FindPlayerByConnectionId(connectionId);

            if (session != null && player != null)
            {
                await _gameSessionService.DisconnectPlayerAsync(connectionId);

                // Powiadom pozostałych graczy
                await Clients.Group(session.GroupName).SendAsync("PlayerEvent", new PlayerEventDto
                {
                    EventType = "disconnected",
                    PlayerId = player.Id,
                    Nickname = player.Nickname,
                    PlayerCount = session.ActivePlayerCount
                });

                _logger.LogInformation("Gracz {Nickname} rozłączony z sesji {SessionId}",
                    player.Nickname, session.Id);
            }

            // Sprawdź czy to host
            var hostedSession = _gameSessionService.GetAllActiveSessions()
                .FirstOrDefault(s => s.HostConnectionId == connectionId);

            if (hostedSession != null && hostedSession.Status != GameStatus.Finished && hostedSession.Status != GameStatus.Cancelled)
            {
                _logger.LogInformation("Host rozłączony z sesji {SessionId} - uruchamiam timeout", hostedSession.Id);

                // Ustaw timeout zamiast natychmiastowego anulowania
                var cts = new CancellationTokenSource();
                _hostDisconnectTimers[hostedSession.Id] = cts;

                // Powiadom graczy że host się rozłączył (ale gra jeszcze nie anulowana)
                await Clients.Group(hostedSession.GroupName).SendAsync("GameMessage", new GameMessageDto
                {
                    Type = "warning",
                    Message = "Host rozłączony - oczekiwanie na ponowne połączenie..."
                });

                // Uruchom timeout w tle
                _ = Task.Run(async () =>
                {
                    try
                    {
                        await Task.Delay(TimeSpan.FromSeconds(GameConstants.PlayerReconnectTimeoutSeconds), cts.Token);

                        // Timeout - anuluj grę
                        if (!cts.Token.IsCancellationRequested)
                        {
                            _logger.LogInformation("Timeout hosta - anulowanie sesji {SessionId}", hostedSession.Id);
                            hostedSession.Status = GameStatus.Cancelled;

                            await Clients.Group(hostedSession.GroupName).SendAsync("GameCancelled", new GameMessageDto
                            {
                                Type = "error",
                                Message = GameConstants.Messages.HostLeft
                            });
                        }
                    }
                    catch (TaskCanceledException)
                    {
                        // Host się połączył - timeout anulowany
                    }
                    finally
                    {
                        _hostDisconnectTimers.TryRemove(hostedSession.Id, out _);
                    }
                });
            }

            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        #region Host Actions

        /// <summary>
        /// Host tworzy sesję gry
        /// </summary>
        public async Task<JoinGameResultDto> CreateGame(int quizId)
        {
            try
            {
                using var scope = _scopeFactory.CreateScope();
                var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var quiz = await dbContext.Quizzes
                    .Include(q => q.Questions)
                    .FirstOrDefaultAsync(q => q.Id == quizId);

                if (quiz == null)
                {
                    return new JoinGameResultDto
                    {
                        Success = false,
                        ErrorMessage = "Quiz nie został znaleziony"
                    };
                }

                var userId = Context.UserIdentifier ?? "";
                var questionIds = quiz.Questions.Select(q => q.Id).ToList();

                if (!questionIds.Any())
                {
                    return new JoinGameResultDto
                    {
                        Success = false,
                        ErrorMessage = "Quiz nie ma żadnych pytań"
                    };
                }

                var session = await _gameSessionService.CreateSessionAsync(
                    quizId,
                    quiz.Title,
                    quiz.AccessCode,
                    Context.ConnectionId,
                    userId,
                    questionIds
                );

                // Dodaj hosta do grupy
                await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);

                _logger.LogInformation("Host utworzył sesję {SessionId} dla quizu {QuizId}",
                    session.Id, quizId);

                return new JoinGameResultDto
                {
                    Success = true,
                    SessionId = session.Id,
                    QuizTitle = quiz.Title,
                    TotalQuestions = questionIds.Count
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd tworzenia gry dla quizu {QuizId}", quizId);
                return new JoinGameResultDto
                {
                    Success = false,
                    ErrorMessage = "Wystąpił błąd podczas tworzenia gry"
                };
            }
        }

        /// <summary>
        /// Host rozpoczyna grę (zmienia status, ale NIE wysyła jeszcze pytań)
        /// </summary>
        public async Task<bool> StartGame(Guid sessionId)
        {
            var (success, errorMessage) = await _gameSessionService.StartGameAsync(sessionId, Context.ConnectionId);

            if (!success)
            {
                await Clients.Caller.SendAsync("GameMessage", new GameMessageDto
                {
                    Type = "error",
                    Message = errorMessage ?? "Nie można rozpocząć gry"
                });
                return false;
            }

            var session = _gameSessionService.GetSession(sessionId);
            if (session == null) return false;

            // Powiadom wszystkich o starcie - wszyscy zostaną przekierowani do Play
            await Clients.Group(session.GroupName).SendAsync("GameStarted");

            _logger.LogInformation("Gra wystartowana w sesji {SessionId} - czekam na połączenie hosta na Play", sessionId);

            return true;
        }

        /// <summary>
        /// Host/Gracz dołącza do sesji gry na stronie Play (po przekierowaniu)
        /// </summary>
        public async Task<bool> JoinGameSession(string sessionIdStr, string? playerIdStr, bool isHost)
        {
            // Parsuj sessionId
            if (!Guid.TryParse(sessionIdStr, out var sessionId))
            {
                _logger.LogWarning("JoinGameSession: Nieprawidłowy sessionId: {SessionId}", sessionIdStr);
                return false;
            }

            var session = _gameSessionService.GetSession(sessionId);
            if (session == null)
            {
                _logger.LogWarning("JoinGameSession: Sesja {SessionId} nie istnieje", sessionId);
                return false;
            }

            // Dodaj do grupy SignalR
            await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);
            _logger.LogInformation("JoinGameSession: Dodano do grupy {GroupName}, isHost={IsHost}, ConnectionId={ConnectionId}",
                session.GroupName, isHost, Context.ConnectionId);

            if (isHost)
            {
                // Anuluj timeout rozłączenia hosta jeśli istnieje
                if (_hostDisconnectTimers.TryRemove(sessionId, out var cts))
                {
                    cts.Cancel();
                    _logger.LogInformation("Host reconnected - timeout anulowany dla sesji {SessionId}", sessionId);
                }

                // Aktualizuj ConnectionId hosta
                session.HostConnectionId = Context.ConnectionId;
                _logger.LogInformation("Host połączony na Play: {SessionId}, ConnectionId: {ConnectionId}",
                    sessionId, Context.ConnectionId);

                // Jeśli gra jest InProgress ale nie ma jeszcze pytania - rozpocznij
                if (session.Status == GameStatus.InProgress && session.CurrentQuestionIndex == -1)
                {
                    _logger.LogInformation("Host gotowy - wysyłam pierwsze pytanie dla sesji {SessionId}", sessionId);
                    await SendNextQuestion(sessionId);
                }
                else if (session.Status == GameStatus.InProgress && session.CurrentQuestionIndex >= 0)
                {
                    // Gra w trakcie - wyślij aktualne pytanie do hosta
                    _logger.LogInformation("Host reconnect w trakcie gry - pytanie {Index}", session.CurrentQuestionIndex);
                }
            }
            else
            {
                // Gracz - parsuj playerId
                Guid? playerId = null;
                if (!string.IsNullOrEmpty(playerIdStr) && Guid.TryParse(playerIdStr, out var parsedPlayerId))
                {
                    playerId = parsedPlayerId;
                }

                if (playerId.HasValue)
                {
                    var player = session.Players.Values.FirstOrDefault(p => p.Id == playerId.Value);
                    if (player != null)
                    {
                        player.ConnectionId = Context.ConnectionId;
                        player.Status = PlayerStatus.Connected;
                        player.DisconnectedAt = null;

                        _logger.LogInformation("Gracz {Nickname} połączony na Play: {SessionId}",
                            player.Nickname, sessionId);
                    }
                    else
                    {
                        _logger.LogWarning("JoinGameSession: Nie znaleziono gracza {PlayerId} w sesji {SessionId}",
                            playerId, sessionId);
                    }
                }
                else
                {
                    _logger.LogWarning("JoinGameSession: Brak playerId dla gracza");
                }
            }

            return true;
        }

        /// <summary>
        /// Host anuluje grę
        /// </summary>
        public async Task CancelGame(Guid sessionId)
        {
            var success = await _gameSessionService.CancelGameAsync(sessionId, Context.ConnectionId);
            if (!success) return;

            var session = _gameSessionService.GetSession(sessionId);
            if (session == null) return;

            await Clients.Group(session.GroupName).SendAsync("GameCancelled", new GameMessageDto
            {
                Type = "info",
                Message = "Gra została anulowana przez hosta"
            });
        }

        #endregion

        #region Player Actions

        /// <summary>
        /// Gracz dołącza do gry
        /// </summary>
        public async Task<JoinGameResultDto> JoinGame(JoinGameDto dto)
        {
            var accessCode = dto.AccessCode.ToUpperInvariant();
            var userId = Context.UserIdentifier;

            var (success, errorMessage, player, isReconnect) = await _gameSessionService.JoinSessionAsync(
                accessCode,
                dto.Nickname,
                Context.ConnectionId,
                userId
            );

            if (!success || player == null)
            {
                return new JoinGameResultDto
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            var session = _gameSessionService.GetSessionByAccessCode(accessCode);
            if (session == null)
            {
                return new JoinGameResultDto
                {
                    Success = false,
                    ErrorMessage = GameConstants.Messages.GameNotFound
                };
            }

            // Dodaj gracza do grupy SignalR
            await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);

            // Powiadom pozostałych - różne zdarzenie dla join vs reconnect
            await Clients.Group(session.GroupName).SendAsync("PlayerEvent", new PlayerEventDto
            {
                EventType = isReconnect ? "reconnected" : "joined",
                PlayerId = player.Id,
                Nickname = player.Nickname,
                PlayerCount = session.ActivePlayerCount
            });

            _logger.LogInformation("Gracz {Nickname} {Action} do sesji {SessionId}",
                player.Nickname, isReconnect ? "powrócił" : "dołączył", session.Id);

            return new JoinGameResultDto
            {
                Success = true,
                SessionId = session.Id,
                PlayerId = player.Id,
                QuizTitle = session.QuizTitle,
                TotalQuestions = session.TotalQuestions
            };
        }

        /// <summary>
        /// Gracz próbuje ponownie dołączyć po rozłączeniu
        /// </summary>
        public async Task<JoinGameResultDto> ReconnectGame(string accessCode, string nickname)
        {
            var (success, session, player) = await _gameSessionService.ReconnectPlayerAsync(
                accessCode.ToUpperInvariant(),
                nickname,
                Context.ConnectionId
            );

            if (!success || session == null || player == null)
            {
                return new JoinGameResultDto
                {
                    Success = false,
                    ErrorMessage = "Nie można ponownie dołączyć do gry"
                };
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, session.GroupName);

            await Clients.Group(session.GroupName).SendAsync("PlayerEvent", new PlayerEventDto
            {
                EventType = "reconnected",
                PlayerId = player.Id,
                Nickname = player.Nickname,
                PlayerCount = session.ActivePlayerCount
            });

            return new JoinGameResultDto
            {
                Success = true,
                SessionId = session.Id,
                PlayerId = player.Id,
                QuizTitle = session.QuizTitle,
                TotalQuestions = session.TotalQuestions
            };
        }

        /// <summary>
        /// Gracz opuszcza grę
        /// </summary>
        public async Task LeaveGame(Guid sessionId, Guid playerId)
        {
            var session = _gameSessionService.GetSession(sessionId);
            if (session == null) return;

            var player = session.Players.Values.FirstOrDefault(p => p.Id == playerId);
            var nickname = player?.Nickname ?? "Nieznany";

            await _gameSessionService.LeaveSessionAsync(sessionId, playerId);
            await Groups.RemoveFromGroupAsync(Context.ConnectionId, session.GroupName);

            await Clients.Group(session.GroupName).SendAsync("PlayerEvent", new PlayerEventDto
            {
                EventType = "left",
                PlayerId = playerId,
                Nickname = nickname,
                PlayerCount = session.ActivePlayerCount
            });
        }

        /// <summary>
        /// Gracz wysyła odpowiedź (obsługuje single i multi-choice)
        /// </summary>
        public async Task<AnswerConfirmationDto> SubmitAnswer(Guid sessionId, Guid playerId, SubmitAnswerDto dto)
        {
            var (success, points, errorMessage) = await _gameSessionService.SubmitAnswerAsync(
                sessionId,
                playerId,
                dto.AnswerIds ?? new List<int>(),
                dto.ResponseTimeSeconds
            );

            if (!success)
            {
                return new AnswerConfirmationDto
                {
                    Success = false,
                    ErrorMessage = errorMessage
                };
            }

            var session = _gameSessionService.GetSession(sessionId);
            if (session == null)
            {
                return new AnswerConfirmationDto { Success = false };
            }

            // Wyślij aktualizację liczby odpowiedzi do hosta
            await Clients.Client(session.HostConnectionId).SendAsync("AnswerCountUpdate", new AnswerCountUpdateDto
            {
                AnsweredCount = session.GetAnsweredCount(),
                TotalPlayers = session.ActivePlayerCount
            });

            // Sprawdź czy wszyscy odpowiedzieli
            if (session.AllPlayersAnswered())
            {
                await EndCurrentQuestion(sessionId);
            }

            return new AnswerConfirmationDto { Success = true };
        }

        #endregion

        #region Game Flow

        /// <summary>
        /// Wysyła następne pytanie do wszystkich graczy
        /// </summary>
        private async Task SendNextQuestion(Guid sessionId)
        {
            var (success, questionId) = await _gameSessionService.NextQuestionAsync(sessionId);
            if (!success || !questionId.HasValue)
            {
                // Koniec pytań - zakończ grę
                await EndGame(sessionId);
                return;
            }

            var session = _gameSessionService.GetSession(sessionId);
            if (session == null) return;

            // Pobierz szczegóły pytania z bazy
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var question = await dbContext.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId.Value);

            if (question == null)
            {
                _logger.LogError("Pytanie {QuestionId} nie znalezione", questionId);
                return;
            }

            // Sprawdź ile poprawnych odpowiedzi ma pytanie
            var correctAnswersCount = question.Answers.Count(a => a.IsCorrect);
            var isMultipleChoice = correctAnswersCount > 1;

            var questionDto = new GameQuestionDto
            {
                QuestionId = question.Id,
                QuestionNumber = session.CurrentQuestionIndex + 1,
                TotalQuestions = session.TotalQuestions,
                Content = question.Content,
                ImageUrl = question.ImageUrl,
                TimeLimitSeconds = question.TimeLimitSeconds,
                MaxPoints = question.Points,
                IsMultipleChoice = isMultipleChoice,
                CorrectAnswersCount = correctAnswersCount,
                Answers = question.Answers.Select(a => new GameAnswerDto
                {
                    AnswerId = a.Id,
                    Content = a.Content
                }).ToList()
            };

            // Wyślij pytanie do wszystkich
            await Clients.Group(session.GroupName).SendAsync("QuestionStarted", questionDto);

            _logger.LogDebug("Wysłano pytanie {QuestionNumber}/{Total} do sesji {SessionId} (multi-choice: {IsMulti})",
                questionDto.QuestionNumber, questionDto.TotalQuestions, sessionId, isMultipleChoice);

            // Ustaw timer na zakończenie pytania
            var timeLimitSeconds = question.TimeLimitSeconds;
            _ = Task.Run(async () =>
            {
                await Task.Delay(TimeSpan.FromSeconds(timeLimitSeconds));

                // Sprawdź czy pytanie jeszcze trwa
                var currentSession = _gameSessionService.GetSession(sessionId);
                if (currentSession?.Status == GameStatus.InProgress &&
                    currentSession.CurrentQuestionId == questionId)
                {
                    await EndCurrentQuestion(sessionId);
                }
            });
        }

        /// <summary>
        /// Kończy aktualne pytanie i pokazuje wyniki
        /// </summary>
        private async Task EndCurrentQuestion(Guid sessionId)
        {
            var session = _gameSessionService.GetSession(sessionId);
            if (session == null || !session.CurrentQuestionId.HasValue) return;

            // Sprawdź czy już nie pokazujemy wyników (unikaj podwójnego wywołania)
            if (session.Status == GameStatus.ShowingResults) return;

            var questionId = session.CurrentQuestionId.Value;
            await _gameSessionService.EndQuestionAsync(sessionId);

            // Pobierz poprawne odpowiedzi
            using var scope = _scopeFactory.CreateScope();
            var dbContext = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

            var question = await dbContext.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null) return;

            // Pobierz wszystkie poprawne odpowiedzi
            var correctAnswerIds = question.Answers
                .Where(a => a.IsCorrect)
                .Select(a => a.Id)
                .ToHashSet();

            if (!correctAnswerIds.Any()) return;

            // Oblicz punkty dla każdego gracza
            foreach (var player in session.Players.Values)
            {
                var answer = player.GetAnswer(questionId);
                if (answer == null) continue;

                // Sprawdź poprawność odpowiedzi
                // Dla multi-choice: gracz musi wybrać DOKŁADNIE te same odpowiedzi co poprawne
                var selectedIds = answer.SelectedAnswerIds.ToHashSet();
                answer.IsCorrect = selectedIds.SetEquals(correctAnswerIds);

                if (answer.IsCorrect)
                {
                    answer.PointsAwarded = _gameSessionService.CalculatePoints(
                        question.Points,
                        answer.ResponseTimeSeconds,
                        question.TimeLimitSeconds
                    );
                    player.TotalScore += answer.PointsAwarded;
                }
            }

            // Wyślij wyniki rundy
            var roundResults = new RoundResultsDto
            {
                QuestionId = questionId,
                CorrectAnswerIds = correctAnswerIds.ToList(),
                NextQuestionInSeconds = GameConstants.ResultsDisplaySeconds
            };

            await Clients.Group(session.GroupName).SendAsync("RoundEnded", roundResults);

            // Wyślij indywidualne wyniki do każdego gracza
            var ranking = session.GetRanking();
            for (int i = 0; i < ranking.Count; i++)
            {
                var player = ranking[i];
                var answer = player.GetAnswer(questionId);

                var playerResult = new PlayerRoundResultDto
                {
                    IsCorrect = answer?.IsCorrect ?? false,
                    PointsAwarded = answer?.PointsAwarded ?? 0,
                    TotalScore = player.TotalScore,
                    CurrentRank = i + 1,
                    ResponseTimeSeconds = answer?.ResponseTimeSeconds ?? 0
                };

                if (!string.IsNullOrEmpty(player.ConnectionId))
                {
                    await Clients.Client(player.ConnectionId).SendAsync("YourRoundResult", playerResult);
                }
            }

            // Wyślij top 5 do wszystkich
            var topPlayers = new TopPlayersDto
            {
                Players = ranking.Take(5).Select((p, i) =>
                {
                    var ans = p.GetAnswer(questionId);
                    return new TopPlayerEntryDto
                    {
                        Rank = i + 1,
                        Nickname = p.Nickname,
                        TotalScore = p.TotalScore,
                        PointsThisRound = ans?.PointsAwarded ?? 0
                    };
                }).ToList()
            };

            await Clients.Group(session.GroupName).SendAsync("TopPlayers", topPlayers);

            // Czekaj i przejdź do następnego pytania
            await Task.Delay(TimeSpan.FromSeconds(GameConstants.ResultsDisplaySeconds));

            // Sprawdź czy sesja nadal istnieje i nie została anulowana
            session = _gameSessionService.GetSession(sessionId);
            if (session == null || session.Status == GameStatus.Cancelled) return;

            session.Status = GameStatus.InProgress;

            if (session.HasNextQuestion)
            {
                await SendNextQuestion(sessionId);
            }
            else
            {
                await EndGame(sessionId);
            }
        }

        /// <summary>
        /// Kończy grę i pokazuje ranking
        /// </summary>
        private async Task EndGame(Guid sessionId)
        {
            await _gameSessionService.FinishGameAsync(sessionId);

            var session = _gameSessionService.GetSession(sessionId);
            if (session == null) return;

            var rankings = _gameSessionService.GetFinalRanking(sessionId);

            var finalRanking = new FinalRankingDto
            {
                QuizTitle = session.QuizTitle,
                TotalQuestions = session.TotalQuestions,
                Rankings = rankings.Select(r => new FinalRankingEntryDto
                {
                    Rank = r.Rank,
                    Nickname = r.Nickname,
                    TotalScore = r.TotalScore,
                    CorrectAnswers = r.CorrectAnswers,
                    AverageResponseTime = r.AverageResponseTime,
                    IsCurrentPlayer = false
                }).ToList()
            };

            // Wyślij ranking do wszystkich (host i gracze)
            await Clients.Group(session.GroupName).SendAsync("GameFinished", finalRanking);

            // Wyślij indywidualne rankingi z oznaczeniem "to ty"
            foreach (var player in session.Players.Values)
            {
                if (string.IsNullOrEmpty(player.ConnectionId)) continue;

                var personalRanking = new FinalRankingDto
                {
                    QuizTitle = session.QuizTitle,
                    TotalQuestions = session.TotalQuestions,
                    Rankings = rankings.Select(r => new FinalRankingEntryDto
                    {
                        Rank = r.Rank,
                        Nickname = r.Nickname,
                        TotalScore = r.TotalScore,
                        CorrectAnswers = r.CorrectAnswers,
                        AverageResponseTime = r.AverageResponseTime,
                        IsCurrentPlayer = r.PlayerId == player.Id
                    }).ToList()
                };

                await Clients.Client(player.ConnectionId).SendAsync("YourFinalRanking", personalRanking);
            }

            _logger.LogInformation("Gra zakończona w sesji {SessionId}", sessionId);
        }

        #endregion

        #region Utility

        /// <summary>
        /// Pobiera informacje o lobby
        /// </summary>
        public LobbyInfoDto? GetLobbyInfo(Guid sessionId)
        {
            var session = _gameSessionService.GetSession(sessionId);
            if (session == null) return null;

            return new LobbyInfoDto
            {
                SessionId = session.Id,
                QuizTitle = session.QuizTitle,
                AccessCode = session.AccessCode,
                TotalQuestions = session.TotalQuestions,
                Players = session.Players.Values.Select(p => new LobbyPlayerDto
                {
                    PlayerId = p.Id,
                    Nickname = p.Nickname,
                    IsConnected = p.Status == PlayerStatus.Connected
                }).ToList()
            };
        }

        #endregion
    }
}
