using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models
{
    /// <summary>
    /// Encja odpowiedzi należącej do pytania
    /// </summary>
    public class Answer
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Treść odpowiedzi jest wymagana")]
        [MaxLength(1000, ErrorMessage = "Treść może mieć maksymalnie 1000 znaków")]
        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;

        // Relacja z pytaniem
        public int QuestionId { get; set; }
        public virtual Question Question { get; set; } = null!;
    }

}
