using QuizApplication.Models;
using QuizApplication.Utilities;
using System.Collections.Concurrent;

namespace QuizApplication.Services
{
    public class GameSessionService : IGameSessionService
    {
        // Klucz: AccessCode (np. "ABC12") -> Wartość: Dane sesji
        private readonly ConcurrentDictionary<string, GameSession> _sessions = new();

        public void InitializeSession(string accessCode, int quizId)
        {

            // Jeśli sesja już istnieje (np. po odświeżeniu), nie twórz nowej, zachowaj stan
            if(_sessions.ContainsKey(accessCode.ToUpper()))
            {
                return;
            }
            // Tworzymy pustą sesję. Host przypisze swoje ConnectionId dopiero jak połączy się przez SignalR
            var session = new GameSession
            {
                QuizId = quizId,
                Players = new List<Player>(),
                HostConnectionId = ""
            };

            // TryAdd - jeśli sesja o takim kodzie już istnieje (np. wisząca), to jej nie nadpisze (można dodać logikę czyszczenia)
            _sessions.TryAdd(accessCode.ToUpper(), session);
        }

        public bool SessionExists(string accessCode)
        {
            return _sessions.ContainsKey(accessCode.ToUpper());
        }

        public bool IsNicknameTaken(string accessCode, string nickname)
        {
            if (_sessions.TryGetValue(accessCode.ToUpper(), out var session))
            {
                lock (session.Players)
                {
                    return session.Players.Any(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase));
                }
            }
            return false;
        }

        public void SetHostConnectionId(string accessCode, string connectionId)
        {
            if (_sessions.TryGetValue(accessCode.ToUpper(), out var session))
            {
                session.HostConnectionId = connectionId;
            }
        }

        public bool AddPlayer(string accessCode, string connectionId, string nickname)
        {
            if (!_sessions.TryGetValue(accessCode.ToUpper(), out var session))
            {
                return false; // Sesja nie istnieje
            }

            lock (session.Players) // Blokada dla bezpieczeństwa listy przy wielu wątkach
            {
                if (session.Players.Any(p => p.Nickname.Equals(nickname, StringComparison.OrdinalIgnoreCase)))
                {
                    return false; // Nick zajęty
                }

                session.Players.Add(new Player
                {
                    ConnectionId = connectionId,
                    Nickname = nickname
                });
            }
            return true;
        }

        public void RemovePlayer(string connectionId)
        {
            foreach (var key in _sessions.Keys)
            {
                if (_sessions.TryGetValue(key, out var session))
                {
                    lock (session.Players)
                    {

                        // Jeśli to Host się rozłączył
                        if (session.HostConnectionId == connectionId)
                        {
                            session.HostConnectionId = string.Empty;
                            return;
                            // Tu można dodać logikę zamykania gry, jeśli Host wyjdzie
                        }
                        var player = session.Players.FirstOrDefault(p => p.ConnectionId == connectionId);
                        if (player != null)
                        {
                            session.Players.Remove(player);
                            // Jeśli pokój jest pusty i nie ma hosta, można usunąć sesję (opcjonalne)
                            return;
                        }


                    }
                }
            }
        }

        public List<string> GetPlayersInSession(string accessCode)
        {
            if (_sessions.TryGetValue(accessCode.ToUpper(), out var session))
            {
                lock (session.Players)
                {
                    return session.Players.Select(p => p.Nickname).ToList();
                }
            }
            return new List<string>();
        }

        public string? GetSessionIdByConnectionId(string connectionId)
        {
            foreach (var entry in _sessions)
            {
                if (entry.Value.HostConnectionId == connectionId ||
                    entry.Value.Players.Any(p => p.ConnectionId == connectionId))
                {
                    return entry.Key;
                }
            }
            return null;
        }

        public bool IsHost(string connectionId)
        {
            foreach (var entry in _sessions.Values)
            {
                if (entry.HostConnectionId == connectionId) return true;
            }
            return false;
        }
    }
}
