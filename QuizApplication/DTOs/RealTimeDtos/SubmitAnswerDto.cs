namespace QuizApplication.DTOs.RealTimeDtos
{
    public class SubmitAnswerDto
    {
        public int QuestionId { get; set; }
        public int AnswerId { get; set; }
        // timeLeft / timeTaken liczy serwer
    }
}
