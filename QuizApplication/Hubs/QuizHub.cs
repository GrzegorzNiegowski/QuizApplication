using Microsoft.AspNetCore.SignalR;
using QuizApplication.Models;
using QuizApplication.Services;
namespace QuizApplication.Hubs
{
    //kontroler" dla WebSocketów.
    public class QuizHub : Hub
    {
        private readonly IGameSessionService _sessionService;

        public QuizHub(GameSessionService sessionService)
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
            await Clients.Caller.SendAsync("UpdatePlayerList", players);
        }

        // Metoda wywoływana przez KLIENTA (JS): joinGamePlayer("KOD123", "Marek")
        public async Task JoinGamePlayer(string accesCode, string nickname)
        {
            accesCode = accesCode.ToUpper();
            if(!_sessionService.SessionExists(accesCode))
            {
                await Clients.Caller.SendAsync("ShowError", "Taki kod gry nie istnieje.");
                return;
            }

            //próba dodania gracza
            bool added = _sessionService.AddPlayer(accesCode, Context.ConnectionId, nickname);
            if(!added)
            {
                await Clients.Caller.SendAsync("ShowError", $"Nick '{nickname}' już jest zajęty w tej grze.");
                return;
            }

            await Groups.AddToGroupAsync(Context.ConnectionId, accesCode);
            // Powiadom WSZYSTKICH w grupie (Hosta i innych graczy) o nowej liście
            var players = _sessionService.GetPlayersInSession(accesCode);
            await Clients.Groups(accesCode).SendAsync("UpdatePlayerList", players);
        }

        // Metoda systemowa: wywoływana, gdy ktoś zamknie przeglądarkę
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var accessCode = _sessionService.GetSessionIdByConnectionId(Context.ConnectionId);

            if (accessCode != null)
            {
                bool isHost = _sessionService.IsHost(Context.ConnectionId);
                _sessionService.RemovePlayer(Context.ConnectionId);

                if (isHost)
                {
                    // Opcjonalnie: Poinformuj graczy, że host wyszedł
                    await Clients.Group(accessCode).SendAsync("HostDisconnected");
                }
                else
                {
                    // Zaktualizuj listę graczy pozostałym
                    var players = _sessionService.GetPlayersInSession(accessCode);
                    await Clients.Group(accessCode).SendAsync("UpdatePlayerList", players);
                }
            }

            await base.OnDisconnectedAsync(exception);
        }

    }
}
