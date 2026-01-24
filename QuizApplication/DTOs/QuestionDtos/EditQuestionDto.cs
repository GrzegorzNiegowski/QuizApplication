using System.ComponentModel.DataAnnotations;

namespace QuizApplication.DTOs.QuestionDtos
{
    /// <summary>
    /// DTO do edycji pytania
    /// </summary>
    public class EditQuestionDto
    {
        public int QuestionId { get; set; }
        public int QuizId { get; set; }

        public string? ImageUrl { get; set; }
        [Required(ErrorMessage = "Treść pytania jest wymagana")]
        [Display(Name = "Treść pytania")]
        [MaxLength(2000, ErrorMessage = "Treść może mieć maksymalnie 2000 znaków")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Czas na odpowiedź (sekundy)")]
        [Range(5, 300, ErrorMessage = "Czas musi wynosić od 5 do 300 sekund")]
        public int TimeLimitSeconds { get; set; } = 30;

        [Display(Name = "Punkty")]
        [Range(0, 5000, ErrorMessage = "Punkty muszą być w zakresie 0-5000")]
        public int Points { get; set; } = 1000;

        public List<AnswerDto> Answers { get; set; } = new();
    }
}
