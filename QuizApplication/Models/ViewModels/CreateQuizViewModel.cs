using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models.ViewModels
{
    public class CreateQuizViewModel
    {
        [Required(ErrorMessage = "Tytuł quizu jest wymagany")]
        [Display(Name = "Tytuł Quizu")]
        [MaxLength(200, ErrorMessage = "Tytuł może mieć max 200 znaków")]
        public string Title { get; set; }
    }
}
