using QuizApplication.DTOs;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.RealTimeDtos;
using QuizApplication.DTOs.SessionDtos;
using QuizApplication.Models;
using QuizApplication.Utilities;
using System.Collections.Concurrent;
using System.Collections.Generic;


namespace QuizApplication.Services
{
    public class GameSessionService : IGameSessionService
    {
        // Klucz: AccessCode (np. "ABC12") -> Wartość: Dane sesji
        private readonly ConcurrentDictionary<string, GameSession> _sessions = new();
        private readonly ConcurrentDictionary<string, string> _connectionToSession = new();


        public void InitializeSession(StartSessionDto dto, GameQuizDto gameQuiz)
        {
            var code = gameQuiz.AccessCode.ToUpper();
            if (_sessions.ContainsKey(code)) return;

            var session = new GameSession
            {
                QuizId = dto.QuizId,
                QuizData = gameQuiz,
                Players = new List<Player>(),
                HostConnectionId = "",
                CurrentQuestionIndex = -1,
                IsGameStarted = false
            };
            _sessions.TryAdd(code, session);
        }

        public bool SessionExists(string sessionCode)
            => !string.IsNullOrEmpty(sessionCode) && _sessions.ContainsKey(sessionCode.ToUpper());

        public JoinSessionResultDto AddPlayer(JoinSessionDto dto, string connectionId)
        {
            var code = dto.SessionCode.ToUpper();
            if (!_sessions.TryGetValue(code, out var session))
            {
                return new JoinSessionResultDto { Success = false, Error = "Sesja nie istnieje" };
            }

            lock (session.Players)
            {
                if (session.Players.Any(p => p.Nickname.Equals(dto.PlayerName, StringComparison.OrdinalIgnoreCase)))
                {
                    return new JoinSessionResultDto { Success = false, Error = "Nick zajęty" };
                }

                // Generujemy ParticipantId jeśli nie przyszło (np. przy zwykłym join)
                var participantId = dto.ParticipantId ?? Guid.NewGuid();

                session.Players.Add(new Player
                {
                    ConnectionId = connectionId,
                    Nickname = dto.PlayerName,
                    Score = 0,
                    // W modelu Player warto dodać pole ParticipantId (Guid), 
                    // ale na razie możemy to pominąć lub mapować w locie.
                });

                _connectionToSession[connectionId] = code;

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
                    if (player != null) _connectionToSession.TryRemove(connectionId, out _);
                }
            }
        }

        public List<PlayerScoreDto> GetPlayersInSession(string sessionCode)
        {
            if (_sessions.TryGetValue(sessionCode.ToUpper(), out var session))
            {
                lock (session.Players)
                {
                    return session.Players.Select(p => new PlayerScoreDto { PlayerName = p.Nickname, Score = p.Score }).ToList();
                }
            }
            return new List<PlayerScoreDto>();
        }

        public string? GetSessionIdByConnectionId(string connectionId) => _connectionToSession.TryGetValue(connectionId, out var accessCode) ? accessCode : null;
        
           
        

        public bool IsHost(string connectionId) => _sessions.Values.Any(s => s.HostConnectionId == connectionId);

        public void SetHostConnectionId(string sessionCode, string connectionId)
        {
            if (_sessions.TryGetValue(sessionCode.ToUpper(), out var session))
            {
                session.HostConnectionId = connectionId;
                _connectionToSession[connectionId] = sessionCode.ToUpperInvariant();
            }
        }

        public bool IsNicknameTaken(string sessionCode, string nickname)
        {
            if (_sessions.TryGetValue(sessionCode.ToUpper(), out var s))
                return s.Players.Any(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
            return false;
        }

        // --- GAME LOGIC ---

        public QuestionForPlayerDto? NextQuestion(string sessionCode)
        {
            if (_sessions.TryGetValue(sessionCode.ToUpper(), out var s))
            {
                s.IsGameStarted = true;
                s.CurrentQuestionIndex++;
                s.AnsweredConnectionIds.Clear();

                if (s.QuizData != null && s.CurrentQuestionIndex < s.QuizData.Questions.Count)
                {
                    var fullQ = s.QuizData.Questions[s.CurrentQuestionIndex];

                    // MAPOWANIE NA BEZPIECZNE DTO (Bez IsCorrect)
                    return new QuestionForPlayerDto
                    {
                        QuestionId = fullQ.Id,
                        Content = fullQ.Content,
                        TimeLimitSeconds = fullQ.TimeLimitSeconds,
                        Points = fullQ.Points,
                        CurrentQuestionIndex = s.CurrentQuestionIndex + 1,
                        TotalQuestions = s.QuizData.Questions.Count,
                        Answers = fullQ.Answers.Select(a => new AnswerForPlayerDto
                        {
                            AnswerId = a.Id,
                            Content = a.Content
                        }).ToList()
                    };
                }
            }
            return null; // Koniec gry
        }


        public OperationResult SubmitAnswer(string sessionCode, string connectionId, SubmitAnswerDto dto)
        {
            var code = sessionCode.ToUpperInvariant();
            if (!_sessions.TryGetValue(code, out var s))
                return OperationResult.Fail("Sesja nie istnieje.");

            // czy gra trwa
            if (!s.IsGameStarted) return OperationResult.Fail("Gra jeszcze się nie rozpoczęła.");

            if (s.CurrentQuestionIndex < 0 || s.QuizData == null || s.CurrentQuestionIndex >= s.QuizData.Questions.Count)
                return OperationResult.Fail("Brak aktywnego pytania.");

            var currentQ = s.QuizData.Questions[s.CurrentQuestionIndex];
            if (currentQ.Id != dto.QuestionId)
                return OperationResult.Fail("To pytanie nie jest aktualne.");

            // czy gracz w sesji
            lock (s.Players)
            {
                if (!s.Players.Any(p => p.ConnectionId == connectionId))
                    return OperationResult.Fail("Nie jesteś graczem w tej sesji.");
            }

            // blokada wielokrotnych odpowiedzi
            lock (s)
            {
                if (s.AnsweredConnectionIds.Contains(connectionId))
                    return OperationResult.Fail("Już odpowiedziałeś na to pytanie.");

                s.AnsweredConnectionIds.Add(connectionId);
            }
            // czy answer należy do pytania
            var answer = currentQ.Answers.FirstOrDefault(a => a.Id == dto.AnswerId);
            if (answer == null)
                return OperationResult.Fail("Nieprawidłowa odpowiedź dla tego pytania.");

            if (answer.IsCorrect)
            {
                lock (s.Players)
                {
                    var p = s.Players.FirstOrDefault(x => x.ConnectionId == connectionId);
                    if (p != null) p.Score += currentQ.Points;
                }
            }

            return OperationResult.Ok();
        }

        public ScoreboardDto GetLeaderboard(string sessionCode)
        {
            var res = new ScoreboardDto();
            if (_sessions.TryGetValue(sessionCode.ToUpper(), out var s))
            {
                lock (s.Players)
                {
                    res.Players = s.Players.OrderByDescending(p => p.Score)
                        .Select(p => new PlayerScoreDto { PlayerName = p.Nickname, Score = p.Score })
                        .ToList();
                }
            }
            return res;
        }

        public bool IsHostOfSession(string sessionCode, string connectionId)
        {
            sessionCode = sessionCode.ToUpperInvariant();
            return _sessions.TryGetValue(sessionCode, out var s) && s.HostConnectionId == connectionId;
        }

        public bool IsPlayerInSession(string sessionCode, string connectionId)
        {
            var code = sessionCode.ToUpperInvariant();
            if (!_sessions.TryGetValue(code, out var s)) return false;
            lock (s.Players)
                return s.Players.Any(p => p.ConnectionId == connectionId);
        }
    }
}
