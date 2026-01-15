namespace QuizApplication.DTOs.SessionDtos
{
    public class JoinSessionResultDto
    {
        public bool Success { get; set; }
        public string? Error { get; set; }
        public Guid? ParticipantId { get; set; }
        public string SessionCode { get; set; } = string.Empty;
    }
}
