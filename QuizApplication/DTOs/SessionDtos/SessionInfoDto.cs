namespace QuizApplication.DTOs.SessionDtos
{
    public class SessionInfoDto
    {
        public string SessionCode { get; set; } = string.Empty;
        public int QuizId { get; set; }
        public string QuizTitle { get; set; } = string.Empty;
    }
}
