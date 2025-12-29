using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models.ViewModels
{
    public class EditQuestionViewModel
    {
        public int QuestionId { get; set; }
        public int QuizId { get; set; }

        [Required(ErrorMessage = "Treść pytania jest wymagana")]
        public string Content { get; set; } = string.Empty;

        [Range(5, 300)]
        public int TimeLimitSeconds { get; set; } = 30;

        [Range(0, 5000)]
        public int Points { get; set; } = 1000;

        public List<EditAnswerViewModel> Answers { get; set; } = new();
    }
}
