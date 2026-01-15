using Microsoft.AspNetCore.SignalR;
using QuizApplication.DTOs.RealTimeDtos;
using QuizApplication.DTOs.SessionDtos;
using QuizApplication.Models;
using QuizApplication.Services;
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
            accesCode = accesCode.ToLower();
            if(!_sessionService.SessionExists(accesCode))
            {
                await Clients.Caller.SendAsync("ShowError", "Sesja nie istnieje. Utwórz ją ponownie.");
                return;
            }

            //Przypoisanie ConnectionId hosta do sesji
            _sessionService.SetHostConnectionId(accesCode, Context.ConnectionId);

            //Dodaj do grupy SignalR
            await Groups.AddToGroupAsync(Context.ConnectionId, accesCode);

            // Wyślij hostowi aktualną listę graczy (jeśli jacyś już czekają)
            var players = _sessionService.GetPlayersInSession(accesCode);
            await Clients.Caller.SendAsync("UpdatePlayerList", players.Select(p => p.PlayerName).ToList());
        }

        public async Task StartGame(string accessCode)
        {
            accessCode = accessCode.ToUpper();
            // Weryfikacja czy to Host (bezpieczeństwo)
            if(!_sessionService.IsHost(Context.ConnectionId))
            {
                return;
            }
            var question = _sessionService.NextQuestion(accessCode); // Zwraca QuestionForPlayerDto
            if (question != null)
            {
                await Clients.Group(accessCode).SendAsync("GameStarted");
                await Clients.Group(accessCode).SendAsync("ShowQuestion", question);
            }
        }

        public async Task RequestNextQuestion(string accessCode)
        {
            if (!_sessionService.IsHost(Context.ConnectionId)) return;
            var question = _sessionService.NextQuestion(accessCode);

            if (question != null)
                await Clients.Group(accessCode).SendAsync("ShowQuestion", question);
            else
            {
                var leaderboard = _sessionService.GetLeaderboard(accessCode);
                await Clients.Group(accessCode).SendAsync("GameOver", leaderboard.Players.ToDictionary(k => k.PlayerName, v => v.Score));
            }
        }

        // Metoda wywoływana przez KLIENTA (JS): joinGamePlayer("KOD123", "Marek")
        public async Task JoinGamePlayer(string accessCode, string nickname)
        {
            var dto = new JoinSessionDto { SessionCode = accessCode, PlayerName = nickname };
            var result = _sessionService.AddPlayer(dto, Context.ConnectionId);

            if (!result.Success)
            {
                await Clients.Caller.SendAsync("ShowError", result.Error);
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, result.SessionCode);
            var players = _sessionService.GetPlayersInSession(result.SessionCode);
            await Clients.Group(result.SessionCode).SendAsync("UpdatePlayerList", players.Select(p => p.PlayerName).ToList());
        }

        public async Task SendAnswer(SubmitAnswerDto dto)
        {
            // ConnectionId bierzemy z kontekstu, reszta jest w DTO
            _sessionService.SubmitAnswer(Context.ConnectionId, dto);

            await Clients.Caller.SendAsync("AnswerAccepted");
        }

        // Metoda systemowa: wywoływana, gdy ktoś zamknie przeglądarkę
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var connectionId = Context.ConnectionId;
            var accessCode = _sessionService.GetSessionIdByConnectionId(connectionId);

            if (!string.IsNullOrEmpty(accessCode))
            {
                bool isHost = _sessionService.IsHost(connectionId);

                if (isHost)
                {
                    await Clients.Group(accessCode).SendAsync("ShowError", "Host zakończył grę.");
                }
                else
                {
                    _sessionService.RemovePlayer(connectionId);
                    var players = _sessionService.GetPlayersInSession(accessCode);
                    await Clients.Group(accessCode).SendAsync("UpdatePlayerList", players);
                }
            }
            await base.OnDisconnectedAsync(exception);
        }

    }
}
