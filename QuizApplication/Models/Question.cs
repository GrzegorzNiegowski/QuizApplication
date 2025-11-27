using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models
{
    public class Question
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Treść pytania jest wymagana")]
        public string Content { get; set; }

        public int TimeLimitSeconds { get; set; } = 30;
        public string? ImageUrl { get; set; }

        public int QuizId { get; set; }
        public virtual Quiz Quiz { get; set; }
        public virtual ICollection<Answer> Answers { get; set; } = new List<Answer>();

    }
}
