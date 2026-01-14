using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.DTOs;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;

namespace QuizApplication.Services
{
    public class QuestionService : IQuestionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IQuizService _quizService;

        public QuestionService(ApplicationDbContext context, IQuizService quizService)
        {
            _context = context;
            _quizService = quizService;
        }

        public async Task<OperationResult> AddQuestionAsync(QuestionDto dto, string userId, bool isAdmin)
        {
            if (!await _quizService.IsOwnerOrAdminAsync(dto.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień.");

            // Walidacja
            if (string.IsNullOrWhiteSpace(dto.Content)) return OperationResult.Fail("Treść jest wymagana.");
            var validAnswers = dto.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Content)).ToList();
            if (validAnswers.Count < 1) return OperationResult.Fail("Minimum 1 odpowiedź wymagana.");
            if (!validAnswers.Any(a => a.IsCorrect)) return OperationResult.Fail("Minimum 1 poprawna odpowiedź.");

            var question = new Question
            {
                QuizId = dto.QuizId,
                Content = dto.Content,
                TimeLimitSeconds = dto.TimeLimitSeconds,
                Points = dto.Points,
                Answers = validAnswers.Select(a => new Answer
                {
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult<QuestionDto>> GetQuestionByIdAsync(int questionId, string userId, bool isAdmin)
        {
            var q = await _context.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == questionId);
            if (q == null) return OperationResult<QuestionDto>.Fail("Nie znaleziono pytania.");

            if (!await _quizService.IsOwnerOrAdminAsync(q.QuizId, userId, isAdmin))
                return OperationResult<QuestionDto>.Fail("Brak uprawnień.");

            var dto = new QuestionDto
            {
                Id = q.Id,
                QuizId = q.QuizId,
                Content = q.Content,
                TimeLimitSeconds = q.TimeLimitSeconds,
                Points = q.Points,
                ImageUrl = q.ImageUrl,
                Answers = q.Answers.Select(a => new AnswerDto
                {
                    Id = a.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };

            return OperationResult<QuestionDto>.Ok(dto);
        }

        public async Task<OperationResult> UpdateQuestionAsync(QuestionDto dto, string userId, bool isAdmin)
        {
            if (dto.Id == null) return OperationResult.Fail("Brak ID pytania.");

            var q = await _context.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (q == null) return OperationResult.Fail("Nie znaleziono pytania.");

            if (!await _quizService.IsOwnerOrAdminAsync(q.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień.");

            q.Content = dto.Content;
            q.TimeLimitSeconds = dto.TimeLimitSeconds;
            q.Points = dto.Points;

            // Wymiana odpowiedzi (najprostsza metoda na uniknięcie konfliktów ID)
            _context.Answers.RemoveRange(q.Answers);
            q.Answers = dto.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Content)).Select(a => new Answer
            {
                QuestionId = q.Id,
                Content = a.Content,
                IsCorrect = a.IsCorrect
            }).ToList();

            if (!q.Answers.Any(a => a.IsCorrect)) return OperationResult.Fail("Musi być min. 1 poprawna odpowiedź.");

            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteQuestionAsync(int questionId, string userId, bool isAdmin)
        {
            var q = await _context.Questions.FindAsync(questionId);
            if (q == null) return OperationResult.Fail("Nie znaleziono pytania");

            var allowed = await _quizService.IsOwnerOrAdminAsync(q.QuizId, userId, isAdmin);
            if (!allowed) return OperationResult.Fail("Brak uprawnień");

            _context.Questions.Remove(q);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult> EditQuestionAsync(QuestionDto dto, string userId, bool isAdmin)
        {
            if (dto.Id == null) return OperationResult.Fail("Brak ID pytania");

            var q = await _context.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == dto.Id);
            if (q == null) return OperationResult.Fail("Pytanie nie istnieje");

            var allowed = await _quizService.IsOwnerOrAdminAsync(q.QuizId, userId, isAdmin);
            if (!allowed) return OperationResult.Fail("Brak uprawnień");

            // Aktualizacja pól pytania
            q.Content = dto.Content;
            q.TimeLimitSeconds = dto.TimeLimitSeconds;
            q.Points = dto.Points;
            // q.ImageUrl = dto.ImageUrl; // Jeśli obsługujesz zmianę obrazka

            // Aktualizacja odpowiedzi (najprościej: usuń stare, dodaj nowe)
            // Wersja zaawansowana: aktualizuj istniejące po ID
            _context.Answers.RemoveRange(q.Answers);

            var newAnswers = dto.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                .Select(a => new Answer
                {
                    QuestionId = q.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList();

            if (!newAnswers.Any()) return OperationResult.Fail("Musi być min. 1 odpowiedź");
            if (!newAnswers.Any(a => a.IsCorrect)) return OperationResult.Fail("Musi być min. 1 poprawna");

            _context.Answers.AddRange(newAnswers);

            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult<QuestionDto>> GetQuestionForEditAsync(int questionId, string userId, bool isAdmin)
        {
            var q = await _context.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == questionId);

            if (q == null) return OperationResult<QuestionDto>.Fail("Pytanie nie istnieje.");

            var allowed = await _quizService.IsOwnerOrAdminAsync(q.QuizId, userId, isAdmin);
            if (!allowed) return OperationResult<QuestionDto>.Fail("Brak uprawnień.");

            // Mapowanie Entcja -> DTO
            var dto = new QuestionDto
            {
                Id = q.Id,
                QuizId = q.QuizId,
                Content = q.Content,
                TimeLimitSeconds = q.TimeLimitSeconds,
                Points = q.Points,
                ImageUrl = q.ImageUrl,
                Answers = q.Answers.OrderBy(a => a.Id).Select(a => new AnswerDto
                {
                    Id = a.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };

            return OperationResult<QuestionDto>.Ok(dto);
        }


        
    }
}
