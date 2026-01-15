namespace QuizApplication.DTOs.RealTimeDtos
{
    public class AnswerForPlayerDto // To, co widzi gracz (BEZ flagi IsCorrect!)
    {
        public int AnswerId { get; set; }
        public string Content { get; set; } = string.Empty;
    }
}
