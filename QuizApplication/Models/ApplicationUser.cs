using Microsoft.AspNetCore.Identity;

namespace QuizApplication.Models
{
    /// <summary>
    /// Rozszerzony użytkownik aplikacji
    /// </summary>
    public class ApplicationUser : IdentityUser
    {
        public virtual ICollection<Quiz> Quizzes { get; set; } = new List<Quiz>();
    }
}
