using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models.ViewModels
{
    public class AnswerViewModel
    {
        [Display(Name = "Treść odpowiedzi")]
        [MaxLength(1000)]
        public string Content { get; set; }=string.Empty;

        [Display(Name = "Poprawna?")]
        public bool IsCorrect { get; set; }
    }
}
