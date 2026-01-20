namespace QuizApplication.DTOs.SessionDtos
{
    public class SessionStatisticsDto
    {
        public int TotalSessions { get; set; }
        public int ActiveGames { get; set; }
        public int WaitingInLobby { get; set; }
        public int TotalPlayers { get; set; }
        public List<SessionInfoDto> ActiveSessions { get; set; } = new();
    }

}

