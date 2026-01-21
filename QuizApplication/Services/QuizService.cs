using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.Validation;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.DTOs.QuizDtos;
using QuizApplication.Models;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;
namespace QuizApplication.Services
{
    public class QuizService : IQuizService
    {
        private readonly ApplicationDbContext _context;

        public QuizService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OperationResult<QuizDetailsDto>> CreateQuizAsync(CreateQuizDto dto)
        {
            var errors = DtoValidators.ValidateCreateQuiz(dto);
            if (errors.Any())
                return OperationResult<QuizDetailsDto>.Fail(errors.ToArray());

            var ownerId = (dto.OwnerId ?? "").Trim();
            if (ownerId.Length == 0)
                return OperationResult<QuizDetailsDto>.Fail("Brak właściciela quizu (OwnerId).");

            var quiz = new Quiz
            {
                Title = dto.Title.Trim(),
                OwnerId = dto.OwnerId,
                AccessCode = await GenerateUniqueAccessCodeAsync(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return await GetQuizDetailsAsync(quiz.Id);
        }

        public async Task<OperationResult<QuizDetailsDto>> GetQuizDetailsAsync(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(qu => qu.Answers)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null)
                return OperationResult<QuizDetailsDto>.Fail("Nie znaleziono quizu.");

            var dto = new QuizDetailsDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                AccessCode = quiz.AccessCode,
                OwnerId = quiz.OwnerId ?? "",
                CreatedAt = quiz.CreatedAt,
                Questions = quiz.Questions.Select(q => new QuestionSummaryDto
                {
                    QuestionId = q.Id,
                    Content = q.Content,
                    TimeLimitSeconds = q.TimeLimitSeconds,
                    Points = q.Points,
                    ImageUrl = q.ImageUrl,
                    Answers = q.Answers.Select(a => new AnswerSummaryDto
                    {
                        Id = a.Id,
                        Content = a.Content,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList()
            };

            return OperationResult<QuizDetailsDto>.Ok(dto);
        }

        public async Task<OperationResult<List<QuizSummaryDto>>> GetAllQuizzesForUserAsync(string userId)
        {
            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.OwnerId == userId)
                .Include(q => q.Questions)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            var dtos = quizzes.Select(q => new QuizSummaryDto
            {
                Id = q.Id,
                Title = q.Title,
                AccessCode = q.AccessCode,
                QuestionCount = q.Questions?.Count ?? 0,
                CreatedAt = q.CreatedAt
            }).ToList();

            return OperationResult<List<QuizSummaryDto>>.Ok(dtos);
        }

        public async Task<OperationResult> UpdateQuizTitleAsync(UpdateQuizDto dto, string userId, bool isAdmin)
        {
            var errors = DtoValidators.ValidateUpdateQuizTitle(dto);
            if (errors.Any())
                return OperationResult.Fail(errors.ToArray());

            var quiz = await _context.Quizzes.FindAsync(dto.Id);
            if (quiz == null)
                return OperationResult.Fail("Quiz nie istnieje.");

            if (!isAdmin && quiz.OwnerId != userId)
                return OperationResult.Fail("Brak uprawnień.");

            quiz.Title = dto.Title.Trim();
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteQuizAsync(int quizId, string userId, bool isAdmin)
        {
            if (quizId <= 0)
                return OperationResult.Fail("Nieprawidłowy identyfikator quizu.");

            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null)
                return OperationResult.Fail("Quiz nie istnieje.");

            if (!isAdmin && quiz.OwnerId != userId)
                return OperationResult.Fail("Brak uprawnień.");

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }

        public async Task<bool> IsOwnerOrAdminAsync(int quizId, string userId, bool isAdmin)
        {
            if (isAdmin) return true;

            var ownerId = await _context.Quizzes
                .Where(q => q.Id == quizId)
                .Select(q => q.OwnerId)
                .FirstOrDefaultAsync();

            return ownerId == userId;
        }

        #region Metody pomocnicze

        /// <summary>
        /// Generuje unikalny 5-znakowy kod dostępu do quizu
        /// </summary>
        private async Task<string> GenerateUniqueAccessCodeAsync()
        {
            string code;
            do
            {
                code = GenerateRandomCode(5);
            }
            while (await _context.Quizzes.AnyAsync(q => q.AccessCode == code));

            return code;
        }

        /// <summary>
        /// Generuje losowy kod o zadanej długości z alfabetu bez mylących znaków
        /// </summary>
        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, length)
                .Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }

        #endregion
    }
}
