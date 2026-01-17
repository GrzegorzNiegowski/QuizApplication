using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.DTOs.QuizDtos;
using QuizApplication.DTOs.RealTimeDtos;
using QuizApplication.DTOs.SessionDtos;
using System.Text.RegularExpressions;

namespace QuizApplication.Validation
{
    public static class DtoValidators
    {
        // ------Quiz----
        public static List<string> ValidateCreateQuiz(CreateQuizDto dto)
        {
            var errors = new List<string>();
            if (dto == null)
            {
                errors.Add("Brak danych");
                return errors;
            }

            var title = (dto.Title ?? "").Trim();
            if (title.Length == 0)
            {
                errors.Add("Tytuł quizu jest wymagany");
            }
            if (title.Length > 100)
            {
                errors.Add("Tytuł quizu nie może przekraczać 200 znaków.");
            }
            return errors;

        }

        public static List<string> ValidateUpdateQuizTitle(UpdateQuizDto dto)
        {
            var errors = new List<string>();
            if (dto == null) { errors.Add("Brak danych."); return errors; }

            if (dto.Id <= 0) errors.Add("Nieprawidłowy identyfikator quizu.");
            var title = (dto.Title ?? "").Trim();
            if (title.Length == 0) errors.Add("Tytuł quizu jest wymagany.");
            if (title.Length > 200) errors.Add("Tytuł quizu nie może przekraczać 200 znaków.");

            return errors;
        }

        // ---------- QUESTION ----------
        public static List<string> ValidateCreateQuestion(CreateQuestionDto dto)
        {
            var errors = new List<string>();
            if (dto == null) { errors.Add("Brak danych."); return errors; }

            if (dto.QuizId <= 0) errors.Add("Nieprawidłowy identyfikator quizu.");

            var content = (dto.Content ?? "").Trim();
            if (content.Length == 0) errors.Add("Treść pytania jest wymagana.");
            if (content.Length > 1000) errors.Add("Treść pytania nie może przekraczać 1000 znaków.");

            if (dto.TimeLimitSeconds < 5 || dto.TimeLimitSeconds > 300)
                errors.Add("Czas na odpowiedź musi wynosić od 5 do 300 sekund.");

            if (dto.Points < 0 || dto.Points > 5000)
                errors.Add("Punkty muszą być w zakresie 0–5000.");

            // Odpowiedzi
            dto.Answers ??= new();
            var answers = dto.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                .Select(a => new { Content = a.Content.Trim(), a.IsCorrect })
                .ToList();

            if (!answers.Any())
                errors.Add("Dodaj co najmniej jedną odpowiedź.");

            if (answers.Any(a => a.Content.Length > 1000))
                errors.Add("Treść odpowiedzi nie może przekraczać 1000 znaków.");

            if (answers.Any() && !answers.Any(a => a.IsCorrect))
                errors.Add("Przynajmniej jedna odpowiedź musi być poprawna.");

            // Opcjonalnie: unikalność odpowiedzi
            var dup = answers.GroupBy(a => a.Content, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1);
            if (dup) errors.Add("Odpowiedzi nie mogą się powtarzać.");

            return errors;
        }

        public static List<string> ValidateEditQuestion(EditQuestionDto dto)
        {
            var errors = new List<string>();
            if (dto == null) { errors.Add("Brak danych."); return errors; }

            if (dto.QuestionId <= 0) errors.Add("Nieprawidłowy identyfikator pytania.");
            if (dto.QuizId <= 0) errors.Add("Nieprawidłowy identyfikator quizu.");

            var content = (dto.Content ?? "").Trim();
            if (content.Length == 0) errors.Add("Treść pytania jest wymagana.");
            if (content.Length > 2000) errors.Add("Treść pytania nie może przekraczać 2000 znaków.");

            if (dto.TimeLimitSeconds < 5 || dto.TimeLimitSeconds > 300)
                errors.Add("Czas na odpowiedź musi wynosić od 5 do 300 sekund.");

            if (dto.Points < 0 || dto.Points > 5000)
                errors.Add("Punkty muszą być w zakresie 0–5000.");

            dto.Answers ??= new();
            var answers = dto.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                .Select(a => new { Content = a.Content.Trim(), a.IsCorrect })
                .ToList();

            if (!answers.Any())
                errors.Add("Dodaj co najmniej jedną odpowiedź.");

            if (answers.Any(a => a.Content.Length > 1000))
                errors.Add("Treść odpowiedzi nie może przekraczać 1000 znaków.");

            if (answers.Any() && !answers.Any(a => a.IsCorrect))
                errors.Add("Przynajmniej jedna odpowiedź musi być poprawna.");

            var dup = answers.GroupBy(a => a.Content, StringComparer.OrdinalIgnoreCase).Any(g => g.Count() > 1);
            if (dup) errors.Add("Odpowiedzi nie mogą się powtarzać.");

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

        public static List<string> ValidateSubmitAnswer(SubmitAnswerDto dto)
        {
            var errors = new List<string>();
            if (dto == null) { errors.Add("Brak danych."); return errors; }

            if (dto.QuestionId <= 0) errors.Add("Nieprawidłowy identyfikator pytania.");
            if (dto.AnswerId <= 0) errors.Add("Nieprawidłowy identyfikator odpowiedzi.");
            return errors;
        }


    }
}
