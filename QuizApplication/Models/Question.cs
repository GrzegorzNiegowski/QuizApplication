using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Treść pytania jest wymagana")]
        public string Content { get; set; } = string.Empty;


        [Range(5, 300, ErrorMessage = "Czas musi wynosić od 5 do 300 sekund")]
        public int TimeLimitSeconds { get; set; } = 30;

        [Range(0, 5000)]
        public int Points { get; set; } = 1000;

        [MaxLength(2000)]
        public string? ImageUrl { get; set; }

        public int QuizId { get; set; }
        public virtual Quiz Quiz { get; set; }
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    }
}
