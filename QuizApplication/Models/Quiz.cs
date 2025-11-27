using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models
{
    public class Quiz
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany")]
        public string Title { get; set; } = "";

        public string AccessCode { get; set; } = Guid.NewGuid().ToString()[..6].ToUpper();

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public string? OwnerId { get; set; }
        public ApplicationUser? Owner { get; set; }

        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();

    }
}
