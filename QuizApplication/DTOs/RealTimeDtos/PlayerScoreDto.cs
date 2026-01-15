namespace QuizApplication.DTOs.RealTimeDtos
{
    public class PlayerScoreDto
    {
        //public Guid ParticipantId { get; set; }
        public string PlayerName { get; set; } = string.Empty;
        public int Score { get; set; }
    }
}
