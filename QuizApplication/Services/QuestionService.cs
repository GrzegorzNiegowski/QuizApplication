using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.DTOs;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;
using QuizApplication.Validation;

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

        public async Task<OperationResult> AddQuestionAsync(CreateQuestionDto dto, string userId, bool isAdmin)
        {
            var errors = DtoValidators.ValidateCreateQuestion(dto);
            if (errors.Any()) return OperationResult.Fail(errors.ToArray());

            if (!await _quizService.IsOwnerOrAdminAsync(dto.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień.");

            var content = dto.Content.Trim();
            var validAnswers = dto.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                .Select(a => new Answer
                {
                    Content = a.Content.Trim(),
                    IsCorrect = a.IsCorrect
                })
                .ToList();

            // (opcjonalnie) max 4 odpowiedzi — jeśli to wymóg biznesowy:
            // if (validAnswers.Count > 4) return OperationResult.Fail("Maksymalnie 4 odpowiedzi.");

            var question = new Question
            {
                QuizId = dto.QuizId,
                Content = content,
                TimeLimitSeconds = dto.TimeLimitSeconds,
                Points = dto.Points,
                Answers = validAnswers
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        /*
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
        */

        public async Task<OperationResult> UpdateQuestionAsync(EditQuestionDto dto, string userId, bool isAdmin)
        {
            var errors = DtoValidators.ValidateEditQuestion(dto);
            if (errors.Any()) return OperationResult.Fail(errors.ToArray());

            var q = await _context.Questions
                .Include(x => x.Answers)
                .FirstOrDefaultAsync(x => x.Id == dto.QuestionId);

            if (q == null) return OperationResult.Fail("Nie znaleziono pytania.");

            // zabezpieczenie przed manipulacją dto.QuizId w request
            if (q.QuizId != dto.QuizId)
                return OperationResult.Fail("Niespójny identyfikator quizu dla pytania.");

            if (!await _quizService.IsOwnerOrAdminAsync(q.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień.");

            q.Content = dto.Content;
            q.TimeLimitSeconds = dto.TimeLimitSeconds;
            q.Points = dto.Points;

            // Najprostsza edycja odpowiedzi: usuń stare, dodaj nowe
            _context.Answers.RemoveRange(q.Answers);

            var newAnswers = dto.Answers.Where(a => !string.IsNullOrWhiteSpace(a.Content)).Select(a => new Answer
            {
                QuestionId = q.Id,
                Content = a.Content,
                IsCorrect = a.IsCorrect
            }).ToList();


            _context.Answers.AddRange(newAnswers);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteQuestionAsync(int questionId, string userId, bool isAdmin)
        {
            if (questionId <= 0) return OperationResult.Fail("Nieprawidłowy identyfikator pytania.");

            var q = await _context.Questions
        .Include(x => x.Answers)
        .FirstOrDefaultAsync(x => x.Id == questionId);

            if (q == null) return OperationResult.Fail("Nie znaleziono.");

            if (!await _quizService.IsOwnerOrAdminAsync(q.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień.");

            _context.Answers.RemoveRange(q.Answers);
            _context.Questions.Remove(q);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }


        /*
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
        */

        public async Task<OperationResult<EditQuestionDto>> GetQuestionForEditAsync(int questionId, string userId, bool isAdmin)
        {
            var q = await _context.Questions.Include(x => x.Answers).FirstOrDefaultAsync(x => x.Id == questionId);
            if (q == null) return OperationResult<EditQuestionDto>.Fail("Nie znaleziono pytania.");

            if (!await _quizService.IsOwnerOrAdminAsync(q.QuizId, userId, isAdmin))
                return OperationResult<EditQuestionDto>.Fail("Brak uprawnień.");

            var dto = new EditQuestionDto
            {
                QuestionId = q.Id,
                QuizId = q.QuizId,
                Content = q.Content,
                TimeLimitSeconds = q.TimeLimitSeconds,
                Points = q.Points,
                Answers = q.Answers.Select(a => new EditAnswerDto
                {
                    Id = a.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };

            return OperationResult<EditQuestionDto>.Ok(dto);
        }



    }
}
