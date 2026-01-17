using QuizApplication.DTOs.GameDtos;
using QuizApplication.Models;
namespace QuizApplication.Utilities
{
    public class GameSession
    {
        // Klasa wewnętrzna trzymająca stan jednej gry
        public int QuizId {  get; set; }
        public string HostConnectionId { get; set; } = string.Empty; // ID połączenia Hosta
        public List<Player> Players { get; set; } = new();
        // Tu w przyszłości dojdzie np. CurrentQuestionIndex
        public GameQuizDto? QuizData { get; set; }
        public bool IsGameStarted { get; set; } = false;
        public int CurrentQuestionIndex { get; set; } = 0;
        public HashSet<string> AnsweredConnectionIds { get; set; } = new();

    }
}
