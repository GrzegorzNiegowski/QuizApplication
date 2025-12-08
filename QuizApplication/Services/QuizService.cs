using QuizApplication.Data;
using QuizApplication.Models.ViewModels;
using QuizApplication.Models;
using QuizApplication.Utilities;
using Microsoft.EntityFrameworkCore;
namespace QuizApplication.Services
{
    public class QuizService : IQuizService
    {
        private readonly ApplicationDbContext _context;

        public QuizService(ApplicationDbContext context)
        {
            _context = context;
        }

        public async Task<OperationResult<Quiz>> CreateQuizAsync(CreateQuizViewModel vm, string ownerId)
        {
            if (vm == null) return OperationResult<Quiz>.Fail("Brak danych");
            if (string.IsNullOrWhiteSpace(vm.Title)) return OperationResult<Quiz>.Fail("Tytuł jest wymagany");
            if (string.IsNullOrWhiteSpace(ownerId)) return OperationResult<Quiz>.Fail("Brak ownerId");
            var quiz = new Quiz
            {
                Title = vm.Title.Trim(),
                OwnerId = ownerId,
                CreatedAt = DateTimeOffset.UtcNow,
                AccessCode = await GenerateUniqueAccessCodeAsync()
            };

            _context.Quizzes.Add(quiz);
            await _context.SaveChangesAsync();
            return OperationResult<Quiz>.Ok(quiz);
        }

        public async Task<OperationResult> DeleteQuizAsync(int quizId, string userId, bool isAdmin)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) return OperationResult.Fail("Quiz nie istnieje");
            if (!await IsOwnerOrAdminAsync(quizId, userId, isAdmin)) return OperationResult.Fail("Brak uprawnień");

            var questionIds = await _context.Questions.Where(q => q.QuizId == quizId).Select(q => q.Id).ToListAsync();
            if(questionIds.Any())
            {
                _context.Answers.RemoveRange(_context.Answers.Where(a => questionIds.Contains(a.QuestionId)));
                _context.Questions.RemoveRange(_context.Questions.Where(q => questionIds.Contains(q.Id)));
            }

            _context.Quizzes.Remove(quiz);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        public async Task<OperationResult<Quiz>> GetQuizWithDetailsAsync(int id)
        {
            var quiz = await _context.Quizzes
                .Include(q => q.Owner)
                .Include(q => q.Questions)
                    .ThenInclude(qn => qn.Answers)
                .FirstOrDefaultAsync(q => q.Id == id);

            if (quiz == null) return OperationResult<Quiz>.Fail("Quiz nie istnieje");
            return OperationResult<Quiz>.Ok(quiz);
        }

        public async Task<bool> IsOwnerOrAdminAsync(int quizId, string userId, bool isAdmin)
        {
            if (isAdmin) return true;
            var quiz = await _context.Quizzes.AsNoTracking().FirstOrDefaultAsync(q => q.Id == quizId);
            if (quiz == null) return false;
            return quiz.OwnerId == userId;
        }

        public async Task<OperationResult> UpdateTitleAsync(int quizId, string newTitle, string userId, bool isAdmin)
        {
            var quiz = await _context.Quizzes.FindAsync(quizId);
            if (quiz == null) return OperationResult.Fail("Quiz nie istnieje;");
            if (!await IsOwnerOrAdminAsync(quizId, userId, isAdmin)) return OperationResult.Fail("Brak uprawnień");
            quiz.Title = newTitle ?? "";
            _context.Quizzes.Update(quiz);
            await _context.SaveChangesAsync();
            return OperationResult.Ok();
        }

        
        private async Task<string> GenerateUniqueAccessCodeAsync()
        {
            string code;
            do
            {
                code = GenerateRandomCode(5);

            } while (await _context.Quizzes.AnyAsync(q => q.AccessCode == code));

            return code;
        }

        private static string GenerateRandomCode(int length) 
        {
            const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
            var rnd = new Random();
            return new string(Enumerable.Range(0,length).Select(_ => chars[rnd.Next(chars.Length)]).ToArray());
        }


    }
}
