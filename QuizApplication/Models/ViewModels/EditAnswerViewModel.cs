namespace QuizApplication.Models.ViewModels
{
    public class EditAnswerViewModel
    {
        public int? Id {  get; set; }
        public string Content { get; set; } = string.Empty;
        public bool IsCorrect { get; set; } = false;
    }
}
