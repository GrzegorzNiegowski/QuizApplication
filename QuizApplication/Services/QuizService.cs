using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.DTOs;
using QuizApplication.DTOs.GameDtos;
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
            var quiz = new Quiz
            {
                Title = dto.Title,
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
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return OperationResult<QuizDetailsDto>.Fail("Nie znaleziono quizu.");

            var dto = new QuizDetailsDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                AccessCode = quiz.AccessCode,
                OwnerId = quiz.OwnerId ?? "",
                Questions = quiz.Questions.Select(q => new QuestionSummaryDto
                {
                    QuestionId = q.Id,
                    Content = q.Content,
                    TimeLimitSeconds = q.TimeLimitSeconds,
                    Points = q.Points
                    
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


        public async Task<bool> IsOwnerOrAdminAsync(int quizId, string userId, bool isAdmin)
        {
            if (isAdmin) return true;
            var ownerId = await _context.Quizzes
                .Where(q => q.Id == quizId)
                .Select(q => q.OwnerId)
                .FirstOrDefaultAsync();
            return ownerId == userId;
        }


        public async Task<OperationResult<GameQuizDto>> GetQuizForGameAsync(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Questions)
                .ThenInclude(qu => qu.Answers)
                .AsNoTracking()
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return OperationResult<GameQuizDto>.Fail("Nie znaleziono.");

            var dto = new GameQuizDto
            {
                Id = quiz.Id,
                Title = quiz.Title,
                AccessCode = quiz.AccessCode,
                Questions = quiz.Questions.Select(q => new GameQuestionDto
                {
                    Id = q.Id,
                    Content = q.Content,
                    TimeLimitSeconds = q.TimeLimitSeconds,
                    Points = q.Points,
                    Answers = q.Answers.Select(a => new GameAnswerDto
                    {
                        Id = a.Id,
                        Content = a.Content,
                        IsCorrect = a.IsCorrect
                    }).ToList()
                }).ToList()
            };

            return OperationResult<GameQuizDto>.Ok(dto);
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
