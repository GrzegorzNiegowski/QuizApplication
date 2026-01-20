using Microsoft.AspNetCore.SignalR;
using QuizApplication.DTOs.RealTimeDtos;
using QuizApplication.DTOs.SessionDtos;
using QuizApplication.Models;
using QuizApplication.Services;
using QuizApplication.Utilities;
using QuizApplication.Validation;
namespace QuizApplication.Hubs
{
    //kontroler" dla WebSocketów.
    public class QuizHub : Hub
    {
        private readonly ILogger<QuizHub> _logger;
        private readonly IGameSessionService _sessionService;

        public QuizHub(IGameSessionService sessionService, ILogger<QuizHub> logger)
        {
            _sessionService = sessionService;
            _logger = logger;
        }

        #region Host Methods

        public async Task JoinGameHost(string accessCode)
        {
            var code = SessionCodeHelper.Normalize(accessCode);

            if (!SessionCodeHelper.IsValid(code))
            {
                await SendErrors("Nieprawidłowy kod gry");
                return;
            }

            if (!_sessionService.SessionExists(code))
            {
                await SendErrors("Sesja o podanym kodzie nie istnieje.");
                return;
            }

            // Przypoisanie ConnectionId hosta do sesji
            _sessionService.SetHostConnectionId(code, Context.ConnectionId);

            // Dodaj do grupy SignalR
            await Groups.AddToGroupAsync(Context.ConnectionId, code);

            _logger.LogInformation("Host joined session {Code} with connection {ConnectionId}",
                code, Context.ConnectionId);

            // Wyślij hostowi aktualną listę graczy (jeśli jacyś już czekają)
            var players = _sessionService.GetPlayersInSession(code);
            await Clients.Caller.SendAsync("UpdatePlayerList", players.Select(p => p.PlayerName).ToList());
        }

        public async Task StartGame(string accessCode)
        {
            var code = SessionCodeHelper.Normalize(accessCode);

            if (!SessionCodeHelper.IsValid(code))
            {
                await SendErrors("Nieprawidłowy kod gry.");
                return;
            }

            if (!_sessionService.SessionExists(code))
            {
                await SendErrors("Sesja o podanym kodzie nie istnieje.");
                return;
            }

            if (!_sessionService.IsHostOfSession(code, Context.ConnectionId))
            {
                await SendErrors("Tylko host może rozpocząć grę.");
                return;
            }

            _logger.LogInformation("Host starting game for session {Code}", code);

            var result = await _sessionService.StartGameAutoAsync(code, Context.ConnectionId);

            if (!result.Success)
            {
                await SendErrors(result.Errors);
            }
        }

        #endregion

        #region Player Methods

        public async Task JoinGamePlayer(string accessCode, string nickname)
        {
            var dto = new JoinSessionDto
            {
                SessionCode = accessCode,
                PlayerName = nickname
            };

            var errors = DtoValidators.ValidateJoinSession(dto);
            if (errors.Any())
            {
                await SendErrors(errors);
                return;
            }

            var code = dto.SessionCode;
            var nick = dto.PlayerName;

            if (!_sessionService.SessionExists(code))
            {
                await SendErrors("Sesja o podanym kodzie nie istnieje.");
                return;
            }

            var result = _sessionService.AddPlayer(dto, Context.ConnectionId);

            if (!result.Success)
            {
                await SendErrors(result.Error ?? "Nie udało się dołączyć do sesji.");
                return;
            }

            _logger.LogInformation("Player {Nick} joined session {Code}", nick, code);

            await Groups.AddToGroupAsync(Context.ConnectionId, result.SessionCode);

            var players = _sessionService.GetPlayersInSession(result.SessionCode);
            await Clients.Group(result.SessionCode).SendAsync("UpdatePlayerList",
                players.Select(p => p.PlayerName).ToList());
        }

        public async Task ReconnectPlayer(string accessCode, Guid participantId)
        {
            var code = SessionCodeHelper.Normalize(accessCode);

            if (!SessionCodeHelper.IsValid(code))
            {
                await SendErrors("Nieprawidłowy kod sesji");
                return;
            }

            if (!_sessionService.SessionExists(code))
            {
                await SendErrors("Sesja nie istnieje");
                return;
            }

            var updated = _sessionService.UpdatePlayerConnection(code, participantId, Context.ConnectionId);

            if (updated)
            {
                await Groups.AddToGroupAsync(Context.ConnectionId, code);
                await Clients.Caller.SendAsync("ReconnectSuccess");

                _logger.LogInformation("Player {ParticipantId} reconnected to session {Code}",
                    participantId, code);
            }
            else
            {
                await SendErrors("Nie udało się ponownie połączyć. Gracz nie znaleziony w sesji.");
            }
        }

        public async Task SendAnswer(string accessCode, SubmitAnswerDto dto)
        {
            var code = SessionCodeHelper.Normalize(accessCode);

            if (!SessionCodeHelper.IsValid(code))
            {
                await SendErrors("Nieprawidłowy kod gry.");
                return;
            }

            if (!_sessionService.SessionExists(code))
            {
                await SendErrors("Sesja o podanym kodzie nie istnieje.");
                return;
            }

            var errors = DtoValidators.ValidateSubmitAnswer(dto);
            if (errors.Any())
            {
                await SendErrors(errors);
                return;
            }

            // Host nie może odpowiadać
            if (_sessionService.IsHostOfSession(code, Context.ConnectionId))
            {
                await SendErrors("Host nie może wysyłać odpowiedzi.");
                return;
            }

            var res = _sessionService.SubmitAnswer(code, Context.ConnectionId, dto);

            if (!res.Success)
            {
                await SendErrors(res.Errors);
                return;
            }

            await Clients.Caller.SendAsync("AnswerAccepted");

            _logger.LogDebug("Answer submitted for question {QuestionId} in session {Code}",
                dto.QuestionId, code);
        }

        #endregion

        #region Connection Lifecycle

        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            var accessCode = _sessionService.GetSessionIdByConnectionId(connectionId);

            if (!string.IsNullOrEmpty(accessCode))
            {
                var code = SessionCodeHelper.Normalize(accessCode);
                bool isHost = _sessionService.IsHostOfSession(code, connectionId);

                if (isHost)
                {
                    _logger.LogWarning("Host disconnected from session {Code}", code);
                    await Clients.Group(code).SendAsync("ShowError",
                        new[] { "Host zakończył grę." });
                }
                else
                {
                    _sessionService.RemovePlayer(connectionId);
                    var players = _sessionService.GetPlayersInSession(code);
                    await Clients.Group(code).SendAsync("UpdatePlayerList",
                        players.Select(p => p.PlayerName).ToList());

                    _logger.LogInformation("Player disconnected from session {Code}", code);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

        #endregion

        #region Helpers

        private Task SendErrors(params string[] errors)
            => Clients.Caller.SendAsync("ShowError", errors);

        private Task SendErrors(IEnumerable<string> errors)
            => Clients.Caller.SendAsync("ShowError", errors.ToArray());

        #endregion

    }
}
