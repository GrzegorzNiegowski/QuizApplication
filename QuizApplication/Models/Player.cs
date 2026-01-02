using System.Security.Permissions;

namespace QuizApplication.Models
{
    public class Player
    {
        public string ConnectionId { get; set; } = string.Empty;
        public string Nickname { get; set; } = string.Empty;
        public int Score { get; set; } = 0;

    }
}
