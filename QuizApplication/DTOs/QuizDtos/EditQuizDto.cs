using System.ComponentModel.DataAnnotations;

namespace QuizApplication.DTOs.QuizDtos
{
    /// <summary>
    /// DTO do edycji quizu
    /// </summary>
    public class EditQuizDto
    {
        public int Id { get; set; }

        [Required(ErrorMessage = "Tytuł jest wymagany")]
        [Display(Name = "Tytuł quizu")]
        [MaxLength(200, ErrorMessage = "Tytuł może mieć maksymalnie 200 znaków")]
        public string Title { get; set; } = string.Empty;
    }
}
