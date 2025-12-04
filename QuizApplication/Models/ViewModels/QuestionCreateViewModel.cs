using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
namespace QuizApplication.Models.ViewModels
{
    public class AnswerCreateModel
    {
        public string Content { get; set; } = "";
        public bool IsCorrect { get; set; } = false;
    }
    public class QuestionCreateViewModel
    {
        public int QuizId { get; set; }

        [Required(ErrorMessage = "Treść pytania jest wymagana")]
        public string Content { get; set; } = "";

        [Range(5, 600, ErrorMessage = "Czas musi być od 5 do 600 sekund")]
        public int TimeLimitSeconds { get; set; } = 30;

        public List<AnswerCreateModel> Answers { get; set; } = new()
        {
            new AnswerCreateModel(),
            new AnswerCreateModel(),
            new AnswerCreateModel(),
            new AnswerCreateModel()
        };

    }
}
