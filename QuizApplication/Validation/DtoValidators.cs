using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.DTOs.QuizDtos;
using QuizApplication.DTOs.RealTimeDtos;
using QuizApplication.DTOs.SessionDtos;
using System.Text.RegularExpressions;

namespace QuizApplication.Validation
{
    /// <summary>
    /// Walidatory dla obiektów DTO
    /// </summary>
    public static class DtoValidators
    {
        /// <summary>
        /// Waliduje DTO tworzenia quizu
        /// </summary>
        public static List<string> ValidateCreateQuiz(CreateQuizDto? dto)
        {
            var errors = new List<string>();

            if (dto == null)
            {
                errors.Add("Brak danych");
                return errors;
            }

            var title = (dto.Title ?? "").Trim();

            if (string.IsNullOrEmpty(title))
                errors.Add("Tytuł quizu jest wymagany");

            if (title.Length > 200)
                errors.Add("Tytuł quizu może mieć maksymalnie 200 znaków");

            return errors;
        }

        /// <summary>
        /// Waliduje DTO edycji quizu
        /// </summary>
        public static List<string> ValidateEditQuiz(EditQuizDto? dto)
        {
            var errors = new List<string>();

            if (dto == null)
            {
                errors.Add("Brak danych");
                return errors;
            }

            if (dto.Id <= 0)
                errors.Add("Nieprawidłowy identyfikator quizu");

            var title = (dto.Title ?? "").Trim();

            if (string.IsNullOrEmpty(title))
                errors.Add("Tytuł quizu jest wymagany");

            if (title.Length > 200)
                errors.Add("Tytuł quizu może mieć maksymalnie 200 znaków");

            return errors;
        }

        /// <summary>
        /// Waliduje DTO tworzenia pytania
        /// </summary>
        public static List<string> ValidateCreateQuestion(CreateQuestionDto? dto)
        {
            var errors = new List<string>();

            if (dto == null)
            {
                errors.Add("Brak danych");
                return errors;
            }

            if (dto.QuizId <= 0)
                errors.Add("Nieprawidłowy identyfikator quizu");

            var content = (dto.Content ?? "").Trim();

            if (string.IsNullOrEmpty(content))
                errors.Add("Treść pytania jest wymagana");

            if (content.Length > 2000)
                errors.Add("Treść pytania może mieć maksymalnie 2000 znaków");

            if (dto.TimeLimitSeconds < 5 || dto.TimeLimitSeconds > 300)
                errors.Add("Czas na odpowiedź musi wynosić od 5 do 300 sekund");

            if (dto.Points < 0 || dto.Points > 5000)
                errors.Add("Punkty muszą być w zakresie 0-5000");

            errors.AddRange(ValidateAnswers(dto.Answers));

            return errors;
        }

        /// <summary>
        /// Waliduje DTO edycji pytania
        /// </summary>
        public static List<string> ValidateEditQuestion(EditQuestionDto? dto)
        {
            var errors = new List<string>();

            if (dto == null)
            {
                errors.Add("Brak danych");
                return errors;
            }

            if (dto.Id <= 0)
                errors.Add("Nieprawidłowy identyfikator pytania");

            if (dto.QuizId <= 0)
                errors.Add("Nieprawidłowy identyfikator quizu");

            var content = (dto.Content ?? "").Trim();

            if (string.IsNullOrEmpty(content))
                errors.Add("Treść pytania jest wymagana");

            if (content.Length > 2000)
                errors.Add("Treść pytania może mieć maksymalnie 2000 znaków");

            if (dto.TimeLimitSeconds < 5 || dto.TimeLimitSeconds > 300)
                errors.Add("Czas na odpowiedź musi wynosić od 5 do 300 sekund");

            if (dto.Points < 0 || dto.Points > 5000)
                errors.Add("Punkty muszą być w zakresie 0-5000");

            errors.AddRange(ValidateAnswers(dto.Answers));

            return errors;
        }

        /// <summary>
        /// Waliduje listę odpowiedzi
        /// </summary>
        private static List<string> ValidateAnswers(List<AnswerDto>? answers)
        {
            var errors = new List<string>();

            if (answers == null || !answers.Any())
            {
                errors.Add("Dodaj co najmniej jedną odpowiedź");
                return errors;
            }

            // Filtruj tylko niepuste odpowiedzi
            var nonEmpty = answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                .ToList();

            if (!nonEmpty.Any())
            {
                errors.Add("Dodaj co najmniej jedną odpowiedź");
                return errors;
            }

            if (nonEmpty.Any(a => a.Content.Trim().Length > 1000))
                errors.Add("Treść odpowiedzi może mieć maksymalnie 1000 znaków");

            if (!nonEmpty.Any(a => a.IsCorrect))
                errors.Add("Przynajmniej jedna odpowiedź musi być poprawna");

            // Sprawdź duplikaty
            var hasDuplicates = nonEmpty
                .GroupBy(a => a.Content.Trim(), StringComparer.OrdinalIgnoreCase)
                .Any(g => g.Count() > 1);

            if (hasDuplicates)
                errors.Add("Odpowiedzi nie mogą się powtarzać");

            return errors;
        }

        // ---------- REALTIME ----------
        public static List<string> ValidateJoinSession(JoinSessionDto dto)
        {
            var errors = new List<string>();
            if (dto == null) { errors.Add("Brak danych."); return errors; }

            dto.SessionCode = (dto.SessionCode ?? "").Trim().ToUpperInvariant();
            dto.PlayerName = (dto.PlayerName ?? "").Trim();

            if (dto.SessionCode.Length == 0) errors.Add("Kod sesji jest wymagany.");
            if (dto.SessionCode.Length > 12) errors.Add("Kod sesji jest zbyt długi.");

            if (dto.PlayerName.Length == 0) errors.Add("Nazwa gracza jest wymagana.");
            if (dto.PlayerName.Length > 20) errors.Add("Nazwa gracza nie może przekraczać 20 znaków.");

            // bezpieczne znaki (minimalnie)
            if (!Regex.IsMatch(dto.PlayerName, @"^[\p{L}\p{N}\s\-_\.]+$"))
                errors.Add("Nazwa gracza zawiera niedozwolone znaki.");

            return errors;
        }



    }
}
