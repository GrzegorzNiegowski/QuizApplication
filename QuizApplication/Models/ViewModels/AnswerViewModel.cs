using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models.ViewModels
{
    public class AnswerViewModel
    {
        [Display(Name = "Treść odpowiedzi")]
        public string Content { get; set; } 

        [Display(Name = "Poprawna?")]
        public bool IsCorrect { get; set; }
    }
}
