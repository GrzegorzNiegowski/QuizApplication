using System.Drawing;

namespace QuizApplication.Services
{
    public interface IGameSessionService
    {
        // Inicjalizuje sesję (robi to Host/Nauczyciel)
        void InitializeSession(string accesCode, int quizId);

        //Sprawdza czy sesja istnieje
        bool SessionExists(string accesCode);
        // Dodaje gracza (zwraca false, jeśli nick jest zajęty)
        bool AddPlayer(string accesCode, string connectionId, string nickname);

        // Usuwa gracza (lub hosta) po rozłączeniu
        void RemovePlayer(string connectionId);

        // Pobiera listę nicków w danej sesji
        List<string> GetPlayersInSession(string accesCode);

        // Pomocnicze: znajdź kod sesji po ID połączenia
        string? GetSessionIdByConnectionId(string connectionId);

        // Sprawdza, czy dany ConnectionId to Host
        bool IsHost(string connectionId);

        // Ustawia ConnectionId dla Hosta (gdy ten połączy się przez SignalR)
        void SetHostConnectionId(string accesCode, string connectionId);
    }
}
