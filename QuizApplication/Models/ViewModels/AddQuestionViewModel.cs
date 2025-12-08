using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models.ViewModels
{
    public class AddQuestionViewModel
    {
        public int QuizId { get; set; }

        [Required(ErrorMessage = "Treść pytania jest wymagana")]
        [Display(Name = "Treść pytania")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Czas na odpowiedź (sekundy)")]
        [Range(5, 300)]
        public int TimeLimitSeconds { get; set; } = 30;

        [Display(Name = "Punkty")]
        [Range(0, 5000)]
        public int Points { get; set; } = 1000;

        // Lista 4 odpowiedzi. 
        // W kontrolerze zainicjalizujemy ją 4 pustymi obiektami, żeby w widoku były 4 inputy.
        public List<AnswerViewModel> Answers { get; set; } = new()
        {
                    new AnswerViewModel(), new AnswerViewModel(), new AnswerViewModel(), new AnswerViewModel()

        };


    }
}
