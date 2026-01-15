namespace QuizApplication.DTOs.QuestionDtos
{
    public class EditAnswerDto
    {
        public int? Id { get; set; } // null = nowa odpowiedź dodana przy edycji
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; }
    }
}
