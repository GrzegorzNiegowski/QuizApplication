using QuizApplication.Models;
using System.Collections.Concurrent;

namespace QuizApplication.Services
{
    public class GameSessionService
    {
        // Klucz: AccessCode (kod quizu), Wartość: Lista graczy
        // Używamy ConcurrentDictionary dla bezpieczeństwa wątków (wielu graczy wbija naraz)
        private readonly ConcurrentDictionary<string, List<Player>> _sessions = new();


        public void AddPlayer(string accesCode, string connectionId, string nickname)
        {
            // Jeśli nie ma takiej sesji, utwórz ją (Host mógł ją utworzyć wcześniej, ale to zabezpieczenie)
            if (!_sessions.ContainsKey(accesCode))
            {
                _sessions.TryAdd(accesCode, new List<Player>());
            }

            var players = _sessions[accesCode];

            // Sprawdź czy taki nick już nie istnieje w tej sesji 
            if (!players.Any(p => p.ConnectionId == connectionId))
            {
                players.Add(new Player { ConnectionId = connectionId, Nickname = nickname });
            }
        }

        public void RemovePlayer(string connectionId)
        {
            // Szukamy gracza we wszystkich sesjach i go usuwamy (np. przy rozłączeniu)
            foreach (var key in _sessions.Keys)
            {
                var player = _sessions[key].FirstOrDefault(p => p.ConnectionId == connectionId);
                if (player != null)
                {
                    {
                        _sessions[key].Remove(player);
                        // Jeśli pokój jest pusty, można go usunąć, ale to zależy od logiki biznesowej
                    }

                }


            }

        }



        public List<string> GetPlayersInSession(string accesCode)
        {
            if(_sessions.TryGetValue(accesCode, out var players))
            {
                return players.Select(p => p.Nickname).ToList();
            }
            return new List<string>();
        }

        // Helper: Znajdź kod sesji po ConnectionId (przydatne przy rozłączaniu)
        public string? GetSessionIdByConnectionId(string connectionId)
        {
            foreach(var entry in _sessions)
            {
                if(entry.Value.Any(p =>p.ConnectionId == connectionId))
                {
                    return entry.Key;
                }
            }
            return null;
        }

    }
}
