using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models
{
    /// <summary>
    /// Encja pytania należącego do quizu
    /// </summary>
    public class Question
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Treść pytania jest wymagana")]
        [MaxLength(2000, ErrorMessage = "Treść może mieć maksymalnie 2000 znaków")]
        public string Content { get; set; } = string.Empty;

        [Range(5, 300, ErrorMessage = "Czas musi wynosić od 5 do 300 sekund")]
        public int TimeLimitSeconds { get; set; } = 30;

        [Range(0, 5000, ErrorMessage = "Punkty muszą być w zakresie 0-5000")]
        public int Points { get; set; } = 1000;

        [MaxLength(2000)]
        public string? ImageUrl { get; set; }

        // Relacja z quizem
        public int QuizId { get; set; }
        public virtual Quiz Quiz { get; set; } = null!;

        // Kolekcja odpowiedzi
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();
    }
}
