using System.ComponentModel.DataAnnotations;

namespace QuizApplication.Models
{
    public class Answer
    {
        public int Id { get; set; }
        [Required]
        public string Content { get; set; } = "";

        public bool IsCorrect { get; set; }

        public int QuestionId { get; set; }
        public virtual Question Question { get; set; }
    }

}
