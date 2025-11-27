using Microsoft.AspNetCore.Identity;

namespace QuizApplication.Models
{
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();  
    }
}
