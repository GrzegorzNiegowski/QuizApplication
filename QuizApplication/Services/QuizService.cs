using QuizApplication.Data;
using QuizApplication.Models.ViewModels;
using QuizApplication.Models;
using QuizApplication.Utilities;
using Microsoft.EntityFrameworkCore;
using QuizApplication.DTOs;
namespace QuizApplication.Services
{
    public class QuizService : IQuizService
    {
        private readonly ApplicationDbContext _context;

        public QuizService(ApplicationDbContext context)
        {
            _context = context;
        }


        public async Task<OperationResult<QuizDto>> CreateQuizAsync(CreateQuizDto dto)
        {
            var quiz = new Quiz
            {
                Title = dto.Title,
                OwnerId = dto.OwnerId,
                AccessCode = await GenerateUniqueAccessCodeAsync(),
                CreatedAt = DateTimeOffset.UtcNow
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();

            return OperationResult<QuizDto>.Ok(MapToDto(quiz));
        }

        public async Task<OperationResult<QuizDto>> GetQuizByIdAsync(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(qu => qu.Answers)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return OperationResult<QuizDto>.Fail("Nie znaleziono quizu.");

            return OperationResult<QuizDto>.Ok(MapToDto(quiz));
        }

        public async Task<OperationResult<List<QuizDto>>> GetAllQuizzesForUserAsync(string userId)
        {
            var quizzes = await _context.Quizzes
                .AsNoTracking()
                .Where(q => q.OwnerId == userId)
                .Include(q => q.Questions)
                .OrderByDescending(q => q.CreatedAt)
                .ToListAsync();

            var dtos = quizzes.Select(q => MapToDto(q)).ToList();
            return OperationResult<List<QuizDto>>.Ok(dtos);
        }

        public async Task<OperationResult> UpdateQuizTitleAsync(UpdateQuizDto dto, string userId, bool isAdmin)
        {
            var quiz = await _context.Quizzes.FindAsync(dto.Id);
            if (quiz == null) return OperationResult.Fail("Quiz nie istnieje.");

            if (!isAdmin && quiz.OwnerId != userId) return OperationResult.Fail("Brak uprawnień.");

            quiz.Title = dto.Title;
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteQuizAsync(int quizId, string userId, bool isAdmin)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) return OperationResult.Fail("Quiz nie istnieje.");

            if (!isAdmin && quiz.OwnerId != userId) return OperationResult.Fail("Brak uprawnień.");

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult<QuizDto>> GetQuizByAccessCodeAsync(string accessCode)
        {
            var quiz = await _context.Quizzes.FirstOrDefaultAsync(q => q.AccessCode == accessCode);
            if (quiz == null) return OperationResult<QuizDto>.Fail("Nie znaleziono quizu.");

            return OperationResult<QuizDto>.Ok(MapToDto(quiz));
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

        // --- Mapper ---
        private static QuizDto MapToDto(Quiz q)
        {
            return new QuizDto
            {
                Id = q.Id,
                Title = q.Title,
                AccessCode = q.AccessCode,
                OwnerId = q.OwnerId ?? "",
                CreatedAt = q.CreatedAt,
                QuestionCount = q.Questions?.Count ?? 0,
                Questions = q.Questions?.Select(qu => new QuestionDto
                {
                    Id = qu.Id,
                    QuizId = qu.QuizId,
                    Content = qu.Content,
                    TimeLimitSeconds = qu.TimeLimitSeconds,
                    Points = qu.Points,
                    ImageUrl = qu.ImageUrl,
                    Answers = qu.Answers?.Select(a => new AnswerDto
                    {
                        Id = a.Id,
                        Content = a.Content,
                        IsCorrect = a.IsCorrect
                    }).ToList() ?? new()
                }).ToList() ?? new()
            };
        }

        private async Task<string> GenerateUniqueAccessCodeAsync()
        {
            string code;
            do { code = GenerateRandomCode(5); }
            while (await _context.Quizzes.AnyAsync(q => q.AccessCode == code));
            return code;
        }
        private static string GenerateRandomCode(int length)
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            return new string(Enumerable.Repeat(chars, length).Select(s => s[Random.Shared.Next(s.Length)]).ToArray());
        }
    }
}
