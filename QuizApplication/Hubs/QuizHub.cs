using Microsoft.AspNetCore.SignalR;
using QuizApplication.Models;
using QuizApplication.Services;
namespace QuizApplication.Hubs
{
    //kontroler" dla WebSocketów.
    public class QuizHub : Hub
    {
        private readonly GameSessionService _sessionService;

        public QuizHub(GameSessionService sessionService)
        {
            _sessionService = sessionService;
        }

        // Metoda wywoływana przez KLIENTA (JS): joinGame("KOD123", "Marek")
        public async Task JounGame(string accesCode, string nickname)
        {
            // 1. Dodaj do grupy SignalIR (dzięki temu wyślemy pytania tylko do tej grupy)
            await Groups.AddToGroupAsync(Context.ConnectionId, accesCode);

            // 2. Zapisz w pamięci serwera
            _sessionService.AddPlayer(accesCode, Context.ConnectionId, nickname);

            // 3. Poinformuj WSZYSTKICH w tej grupie (Hosta też), że ktoś dołączył
            // Wywołujemy metodę JS o nazwie "UpdatePlayerList"
            var players = _sessionService.GetPlayersInSession(accesCode);
            await Clients.Group(accesCode).SendAsync("UpdatePlayerList",players);
        }

        // Metoda systemowa: wywoływana, gdy ktoś zamknie przeglądarkę
        public override async Task OnDisconnectedAsync(Exception? exception)
        {
            var accesCode = _sessionService.GetSessionIdByConnectionId(Context.ConnectionId);
            if(accesCode!=null)
            {
                _sessionService.RemovePlayer(Context.ConnectionId);
                var players = _sessionService.GetPlayersInSession(accesCode);
                await Clients.Group(accesCode).SendAsync("UpdatePlayerList", players);
            }

            await base.OnDisconnectedAsync(exception);
        }

    }
}
