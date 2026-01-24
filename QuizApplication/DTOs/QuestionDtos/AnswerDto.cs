using System.ComponentModel.DataAnnotations;

namespace QuizApplication.DTOs.QuestionDtos
{
    /// <summary>
    /// DTO odpowiedzi (uniwersalne - tworzenie, edycja, wyświetlanie)
    /// </summary>
    public class AnswerDto
    {
        public int? Id { get; set; }

        [Display(Name = "Treść odpowiedzi")]
        [MaxLength(1000, ErrorMessage = "Treść może mieć maksymalnie 1000 znaków")]
        public string Content { get; set; } = string.Empty;

        [Display(Name = "Poprawna?")]
        public bool IsCorrect { get; set; }
    }
}
