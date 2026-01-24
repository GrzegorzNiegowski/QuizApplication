using System.ComponentModel.DataAnnotations;

namespace QuizApplication.DTOs.QuestionDtos
{
    /// <summary>
    /// DTO do tworzenia nowego pytania
    /// </summary>
    public class CreateQuestionDto
    {
        public int QuizId { get; set; }

        [Required(ErrorMessage = "Treść pytania jest wymagana")]
        [Display(Name = "Treść pytania")]
        [MaxLength(2000, ErrorMessage = "Treść może mieć maksymalnie 2000 znaków")]
        public string Content { get; set; } = string.Empty;

        public string? ImageUrl { get; set; }

        [Display(Name = "Czas na odpowiedź (sekundy)")]
        [Range(5, 300, ErrorMessage = "Czas musi wynosić od 5 do 300 sekund")]
        public int TimeLimitSeconds { get; set; } = 30;

        [Display(Name = "Punkty")]
        [Range(0, 5000, ErrorMessage = "Punkty muszą być w zakresie 0-5000")]
        public int Points { get; set; } = 1000;

        // Domyślnie 4 puste odpowiedzi
        public List<AnswerDto> Answers { get; set; } = new()
        {
            new AnswerDto(), new AnswerDto(), new AnswerDto(), new AnswerDto()
        };


    }
}
