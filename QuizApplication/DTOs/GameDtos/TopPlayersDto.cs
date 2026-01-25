namespace QuizApplication.DTOs.GameDtos
{
    /// <summary>
    /// Top graczy po rundzie
    /// </summary>
    public class TopPlayersDto
    {
        public List<TopPlayerEntryDto> Players { get; set; } = new();
    }
}
