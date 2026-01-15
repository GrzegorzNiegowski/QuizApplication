namespace QuizApplication.DTOs.SessionDtos
{
    public class JoinSessionDto
    {
        public string SessionCode { get; set; } = string.Empty;
        public string PlayerName { get; set; } = string.Empty;
        public Guid? ParticipantId { get; set; } = null;
    }
}
