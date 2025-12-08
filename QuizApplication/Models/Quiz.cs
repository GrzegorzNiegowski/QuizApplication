using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models
{
    public class Quiz
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany")]
        [MaxLength(200)]
        public string Title { get; set; } = string.Empty;


        [MaxLength(5)]
        public string AccessCode { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    }
}
