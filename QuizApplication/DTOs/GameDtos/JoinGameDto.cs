using System.ComponentModel.DataAnnotations;
using QuizApplication.Utilities;

namespace QuizApplication.DTOs.GameDtos
{
    // === Dołączanie do gry ===

    /// <summary>
    /// DTO dołączania do gry
    /// </summary>
    public class JoinGameDto
    {
        [Required(ErrorMessage = "Kod gry jest wymagany")]
        [StringLength(GameConstants.AccessCodeLength, MinimumLength = GameConstants.AccessCodeLength,
            ErrorMessage = "Nieprawidłowy kod gry")]
        [RegularExpression(@"^[A-Z0-9]+$", ErrorMessage = "Nieprawidłowy kod gry")]
        public string AccessCode { get; set; } = string.Empty;

        [Required(ErrorMessage = "Nick jest wymagany")]
        [StringLength(GameConstants.MaxNicknameLength, MinimumLength = GameConstants.MinNicknameLength,
            ErrorMessage = "Nick musi mieć od 2 do 20 znaków")]
        [RegularExpression(@"^[a-zA-Z0-9_żźćńółęąśŻŹĆĄŚĘŁÓŃ]+$",
            ErrorMessage = "Nick może zawierać tylko litery, cyfry i _")]
        public string Nickname { get; set; } = string.Empty;
    }
}