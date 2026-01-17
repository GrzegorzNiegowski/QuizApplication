using Microsoft.AspNetCore.SignalR;
using QuizApplication.DTOs.RealTimeDtos;
using QuizApplication.DTOs.SessionDtos;
using QuizApplication.Models;
using QuizApplication.Services;
using QuizApplication.Validation;
namespace QuizApplication.Hubs
{
    //kontroler" dla WebSocketów.
    public class QuizHub : Hub
    {
        private readonly IGameSessionService _sessionService;

        public QuizHub(IGameSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        //Metoda dla Hosta
        public async Task JoinGameHost(string accesCode)
        {
            var code = NormCode(accesCode);
            if(string.IsNullOrWhiteSpace(code))
            {
                await SendErrors("Kod gry jest wymagany");
                return;
            }

            if(!_sessionService.SessionExists(code))
            {
                await SendErrors("Sesja o podanym kodzie nie istnieje.");
                return;
            }

            //Przypoisanie ConnectionId hosta do sesji
            _sessionService.SetHostConnectionId(code, Context.ConnectionId);

            //Dodaj do grupy SignalR
            await Groups.AddToGroupAsync(Context.ConnectionId, code);

            // Wyślij hostowi aktualną listę graczy (jeśli jacyś już czekają)
            var players = _sessionService.GetPlayersInSession(code);
            await Clients.Caller.SendAsync("UpdatePlayerList", players.Select(p => p.PlayerName).ToList());
        }

        public async Task StartGame(string accessCode)
        {
            var code = NormCode(accessCode);
            if (string.IsNullOrWhiteSpace(code))
            {
                await SendErrors("Kod gry jest wymagany.");
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

            var question = _sessionService.NextQuestion(code);
            if (question != null)
            {
                await Clients.Group(code).SendAsync("GameStarted");
                await Clients.Group(code).SendAsync("ShowQuestion", question);
            }
            else
            {
                await SendErrors("Brak pytań w quizie.");
            }
        }

        public async Task RequestNextQuestion(string accessCode)
        {
            var code = NormCode(accessCode);
            if (string.IsNullOrWhiteSpace(code))
            {
                await SendErrors("Kod gry jest wymagany.");
                return;
            }

            if (!_sessionService.SessionExists(code))
            {
                await SendErrors("Sesja o podanym kodzie nie istnieje.");
                return;
            }

            if (!_sessionService.IsHostOfSession(code, Context.ConnectionId))
            {
                await SendErrors("Tylko host może sterować pytaniami.");
                return;
            }

            var question = _sessionService.NextQuestion(code);
            if (question != null)
            {
                await Clients.Group(code).SendAsync("ShowQuestion", question);
                return;
            }

            var leaderboard = _sessionService.GetLeaderboard(code);
            await Clients.Group(code).SendAsync("GameOver",
                leaderboard.Players.ToDictionary(k => k.PlayerName, v => v.Score));
        }

        // Metoda wywoływana przez KLIENTA (JS): joinGamePlayer("KOD123", "Marek")
        public async Task JoinGamePlayer(string accessCode, string nickname)
        {
            var dto = new JoinSessionDto { SessionCode = accessCode, PlayerName = nickname };

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

            await Groups.AddToGroupAsync(Context.ConnectionId, result.SessionCode);
            var players = _sessionService.GetPlayersInSession(result.SessionCode);
            await Clients.Group(result.SessionCode).SendAsync("UpdatePlayerList", players.Select(p => p.PlayerName).ToList());
        }

        public async Task SendAnswer(string accessCode, SubmitAnswerDto dto)
        {
            var code = NormCode(accessCode);
            if (string.IsNullOrWhiteSpace(code))
            {
                await SendErrors("Kod gry jest wymagany.");
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

            // domenowo: host nie odpowiada
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
        }

        // Metoda systemowa: wywoływana, gdy ktoś zamknie przeglądarkę
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            var accessCode = _sessionService.GetSessionIdByConnectionId(connectionId);

            if (!string.IsNullOrEmpty(accessCode))
            {
                bool isHost = _sessionService.IsHostOfSession(accessCode, connectionId);

                if (isHost)
                {
                    await Clients.Group(accessCode).SendAsync("ShowError", "Host zakończył grę.");
                }
                else
                {
                    _sessionService.RemovePlayer(connectionId);
                    var players = _sessionService.GetPlayersInSession(accessCode);
                    await Clients.Group(accessCode).SendAsync("UpdatePlayerList", players.Select(p => p.PlayerName).ToList());
                }
            }
            await base.OnDisconnectedAsync(exception);
        }


        //helpoery

        private static string NormCode(string? accessCode) => (accessCode ?? "").Trim().ToUpperInvariant();
        private static string NormNick(string nickName) => (nickName ?? "").Trim();

        private Task SendErrors(params string[] errors)
    => Clients.Caller.SendAsync("ShowError", errors);

        private Task SendErrors(IEnumerable<string> errors)
            => Clients.Caller.SendAsync("ShowError", errors.ToArray());

    }
}
