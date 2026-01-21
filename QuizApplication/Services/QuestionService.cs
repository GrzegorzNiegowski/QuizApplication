using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.Models;
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
            if (errors.Any())
                return OperationResult.Fail(errors.ToArray());

            if (!await _quizService.IsOwnerOrAdminAsync(dto.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień.");

            var validAnswers = dto.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                .Select(a => new Answer
                {
                    Content = a.Content.Trim(),
                    IsCorrect = a.IsCorrect
                })
                .ToList();

            var question = new Question
            {
                QuizId = dto.QuizId,
                Content = dto.Content.Trim(),
                TimeLimitSeconds = dto.TimeLimitSeconds,
                Points = dto.Points,
                ImageUrl = dto.ImageUrl,
                Answers = validAnswers
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }

        public async Task<OperationResult<EditQuestionDto>> GetQuestionForEditAsync(int questionId, string userId, bool isAdmin)
        {
            var question = await _context.Questions
                .Include(x => x.Answers)
                .FirstOrDefaultAsync(x => x.Id == questionId);

            if (question == null)
                return OperationResult<EditQuestionDto>.Fail("Nie znaleziono pytania.");

            if (!await _quizService.IsOwnerOrAdminAsync(question.QuizId, userId, isAdmin))
                return OperationResult<EditQuestionDto>.Fail("Brak uprawnień.");

            var dto = new EditQuestionDto
            {
                QuestionId = question.Id,
                QuizId = question.QuizId,
                Content = question.Content,
                TimeLimitSeconds = question.TimeLimitSeconds,
                Points = question.Points,
                ImageUrl = question.ImageUrl,
                Answers = question.Answers.Select(a => new EditAnswerDto
                {
                    Id = a.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };

            return OperationResult<EditQuestionDto>.Ok(dto);
        }

        public async Task<OperationResult> UpdateQuestionAsync(EditQuestionDto dto, string userId, bool isAdmin)
        {
            var errors = DtoValidators.ValidateEditQuestion(dto);
            if (errors.Any())
                return OperationResult.Fail(errors.ToArray());

            var question = await _context.Questions
                .Include(x => x.Answers)
                .FirstOrDefaultAsync(x => x.Id == dto.QuestionId);

            if (question == null)
                return OperationResult.Fail("Nie znaleziono pytania.");

            // Zabezpieczenie przed manipulacją dto.QuizId w żądaniu
            if (question.QuizId != dto.QuizId)
                return OperationResult.Fail("Niespójny identyfikator quizu dla pytania.");

            if (!await _quizService.IsOwnerOrAdminAsync(question.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień.");

            // Aktualizacja właściwości pytania
            question.Content = dto.Content.Trim();
            question.TimeLimitSeconds = dto.TimeLimitSeconds;
            question.Points = dto.Points;
            question.ImageUrl = dto.ImageUrl;

            // Usunięcie starych odpowiedzi i dodanie nowych
            _context.Answers.RemoveRange(question.Answers);

            var newAnswers = dto.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                .Select(a => new Answer
                {
                    QuestionId = question.Id,
                    Content = a.Content.Trim(),
                    IsCorrect = a.IsCorrect
                }).ToList();

            _context.Answers.AddRange(newAnswers);
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteQuestionAsync(int questionId, string userId, bool isAdmin)
        {
            if (questionId <= 0)
                return OperationResult.Fail("Nieprawidłowy identyfikator pytania.");

            var question = await _context.Questions
                .Include(x => x.Answers)
                .FirstOrDefaultAsync(x => x.Id == questionId);

            if (question == null)
                return OperationResult.Fail("Nie znaleziono pytania.");

            if (!await _quizService.IsOwnerOrAdminAsync(question.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień.");

            _context.Answers.RemoveRange(question.Answers);
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }



    }
}
