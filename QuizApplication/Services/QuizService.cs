using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.Validation;
using QuizApplication.DTOs.GameDtos;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.DTOs.QuizDtos;
using QuizApplication.Models;
using QuizApplication.Utilities;
namespace QuizApplication.Services
{
   
        /// <summary>
        /// Implementacja serwisu do zarządzania quizami
        /// </summary>
        public class QuizService : IQuizService
        {
            private readonly ApplicationDbContext _context;

            public QuizService(ApplicationDbContext context)
            {
                _context = context;
            }

            public async Task<OperationResult<QuizDetailsDto>> CreateAsync(CreateQuizDto dto)
            {
                // Walidacja danych wejściowych
                var errors = DtoValidators.ValidateCreateQuiz(dto);
                if (errors.Any())
                    return OperationResult<QuizDetailsDto>.Fail(errors.ToArray());

                var ownerId = (dto.OwnerId ?? "").Trim();
                if (string.IsNullOrEmpty(ownerId))
                    return OperationResult<QuizDetailsDto>.Fail("Brak właściciela quizu");

                // Tworzenie nowego quizu
                var quiz = new Quiz
                {
                    Title = dto.Title.Trim(),
                    OwnerId = ownerId,
                    AccessCode = await GenerateUniqueAccessCodeAsync(),
                    CreatedAt = DateTimeOffset.UtcNow
                };

                _context.Quizzes.Add(quiz);
                await _context.SaveChangesAsync();

                return await GetByIdAsync(quiz.Id);
            }

            public async Task<OperationResult<QuizDetailsDto>> GetByIdAsync(int id)
            {
                var quiz = await _context.Quizzes
                    .Include(q => q.Questions)
                        .ThenInclude(q => q.Answers)
                    .AsNoTracking()
                    .FirstOrDefaultAsync(q => q.Id == id);

                if (quiz == null)
                    return OperationResult<QuizDetailsDto>.Fail("Nie znaleziono quizu");

                // Mapowanie encji na DTO
                var dto = new QuizDetailsDto
                {
                    Id = quiz.Id,
                    Title = quiz.Title,
                    AccessCode = quiz.AccessCode,
                    OwnerId = quiz.OwnerId,
                    CreatedAt = quiz.CreatedAt,
                    Questions = quiz.Questions
                        .OrderBy(q => q.Id)
                        .Select(q => new QuestionDto
                        {
                            Id = q.Id,
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
                        }).ToList()
                };

                return OperationResult<QuizDetailsDto>.Ok(dto);
            }

            public async Task<OperationResult<List<QuizListDto>>> GetAllForUserAsync(string userId)
            {
                var quizzes = await _context.Quizzes
                    .AsNoTracking()
                    .Where(q => q.OwnerId == userId)
                    .Include(q => q.Questions)
                    .OrderByDescending(q => q.CreatedAt)
                    .Select(q => new QuizListDto
                    {
                        Id = q.Id,
                        Title = q.Title,
                        AccessCode = q.AccessCode,
                        QuestionCount = q.Questions.Count,
                        CreatedAt = q.CreatedAt
                    })
                    .ToListAsync();

                return OperationResult<List<QuizListDto>>.Ok(quizzes);
            }

            public async Task<OperationResult> UpdateAsync(EditQuizDto dto, string userId, bool isAdmin)
            {
                // Walidacja danych wejściowych
                var errors = DtoValidators.ValidateEditQuiz(dto);
                if (errors.Any())
                    return OperationResult.Fail(errors.ToArray());

                var quiz = await _context.Quizzes.FindAsync(dto.Id);
                if (quiz == null)
                    return OperationResult.Fail("Quiz nie istnieje");

                // Sprawdzenie uprawnień
                if (!isAdmin && quiz.OwnerId != userId)
                    return OperationResult.Fail("Brak uprawnień do edycji tego quizu");

                quiz.Title = dto.Title.Trim();
                await _context.SaveChangesAsync();

                return OperationResult.Ok();
            }

            public async Task<OperationResult> DeleteAsync(int quizId, string userId, bool isAdmin)
            {
                if (quizId <= 0)
                    return OperationResult.Fail("Nieprawidłowy identyfikator quizu");

                var quiz = await _context.Quizzes.FindAsync(quizId);
                if (quiz == null)
                    return OperationResult.Fail("Quiz nie istnieje");

                // Sprawdzenie uprawnień
                if (!isAdmin && quiz.OwnerId != userId)
                    return OperationResult.Fail("Brak uprawnień do usunięcia tego quizu");

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

            #region Metody prywatne

            /// <summary>
            /// Generuje unikalny kod dostępu do quizu
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
            /// Generuje losowy kod o podanej długości (bez mylących znaków 0, O, I, l, 1)
            /// </summary>
            private static string GenerateRandomCode(int length)
            {
                const string chars = "ABCDEFGHJKMNPQRSTUVWXYZ23456789";
                return new string(Enumerable
                    .Repeat(chars, length)
                    .Select(s => s[Random.Shared.Next(s.Length)])
                    .ToArray());
            }

            #endregion
        }
    }
