using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models
{
    /// <summary>
    /// Encja quizu - główna jednostka zawierająca pytania
    /// </summary>
    public class Quiz
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany")]
        [MaxLength(200, ErrorMessage = "Tytuł może mieć maksymalnie 200 znaków")]
        public string Title { get; set; } = string.Empty;

        [MaxLength(6)]
        public string AccessCode { get; set; } = string.Empty;

        public DateTimeOffset CreatedAt { get; set; } = DateTimeOffset.UtcNow;

        // Relacja z właścicielem
        public string? OwnerId { get; set; }
        public virtual ApplicationUser? Owner { get; set; }

        // Kolekcja pytań
        public virtual ICollection<Question> Questions { get; set; } = new List<Question>();
    }
}
