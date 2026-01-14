namespace QuizApplication.DTOs
{
    public class AnswerDto
    {
        public int? Id { get; set; } // Null przy dodawaniu, Int przy edycji
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } 
    }
}
