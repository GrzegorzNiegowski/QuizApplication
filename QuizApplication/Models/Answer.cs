using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models
{
    public class Answer
    {
        public int Id { get; set; }
        [Required, MaxLength(1000)]
        public string Content { get; set; } = string.Empty;

        public bool IsCorrect { get; set; } = false;

        public int QuestionId { get; set; }
        public virtual Question? Question { get; set; }
    }

}
