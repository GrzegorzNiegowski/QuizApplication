using Microsoft.EntityFrameworkCore;
using QuizApplication.Data;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.Models;
using QuizApplication.Utilities;
using QuizApplication.Validation;


namespace QuizApplication.Services
{
    /// <summary>
    /// Implementacja serwisu do zarządzania pytaniami
    /// </summary>
    public class QuestionService : IQuestionService
    {
        private readonly ApplicationDbContext _context;
        private readonly IQuizService _quizService;

        public QuestionService(ApplicationDbContext context, IQuizService quizService)
        {
            _context = context;
            _quizService = quizService;
        }

        public async Task<OperationResult> CreateAsync(CreateQuestionDto dto, string userId, bool isAdmin)
        {
            // Walidacja danych wejściowych
            var errors = DtoValidators.ValidateCreateQuestion(dto);
            if (errors.Any())
                return OperationResult.Fail(errors.ToArray());

            // Sprawdzenie uprawnień
            if (!await _quizService.IsOwnerOrAdminAsync(dto.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień do dodawania pytań do tego quizu");

            // Filtrowanie niepustych odpowiedzi
            var answers = dto.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                .Select(a => new Answer
                {
                    Content = a.Content.Trim(),
                    IsCorrect = a.IsCorrect
                })
                .ToList();

            // Tworzenie nowego pytania
            var question = new Question
            {
                QuizId = dto.QuizId,
                Content = dto.Content.Trim(),
                TimeLimitSeconds = dto.TimeLimitSeconds,
                Points = dto.Points,
                Answers = answers
            };

            _context.Questions.Add(question);
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }

        public async Task<OperationResult<EditQuestionDto>> GetForEditAsync(int questionId, string userId, bool isAdmin)
        {
            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
                return OperationResult<EditQuestionDto>.Fail("Nie znaleziono pytania");

            // Sprawdzenie uprawnień
            if (!await _quizService.IsOwnerOrAdminAsync(question.QuizId, userId, isAdmin))
                return OperationResult<EditQuestionDto>.Fail("Brak uprawnień do edycji tego pytania");

            // Mapowanie encji na DTO
            var dto = new EditQuestionDto
            {
                QuestionId = question.Id,
                QuizId = question.QuizId,
                Content = question.Content,
                TimeLimitSeconds = question.TimeLimitSeconds,
                Points = question.Points,
                Answers = question.Answers.Select(a => new AnswerDto
                {
                    Id = a.Id,
                    Content = a.Content,
                    IsCorrect = a.IsCorrect
                }).ToList()
            };

            return OperationResult<EditQuestionDto>.Ok(dto);
        }

        public async Task<OperationResult> UpdateAsync(EditQuestionDto dto, string userId, bool isAdmin)
        {
            // Walidacja danych wejściowych
            var errors = DtoValidators.ValidateEditQuestion(dto);
            if (errors.Any())
                return OperationResult.Fail(errors.ToArray());

            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == dto.QuestionId);

            if (question == null)
                return OperationResult.Fail("Nie znaleziono pytania");

            // Zabezpieczenie przed manipulacją QuizId
            if (question.QuizId != dto.QuizId)
                return OperationResult.Fail("Niespójny identyfikator quizu");

            // Sprawdzenie uprawnień
            if (!await _quizService.IsOwnerOrAdminAsync(question.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień do edycji tego pytania");

            // Aktualizacja pól pytania
            question.Content = dto.Content.Trim();
            question.TimeLimitSeconds = dto.TimeLimitSeconds;
            question.Points = dto.Points;

            // Usunięcie starych i dodanie nowych odpowiedzi
            _context.Answers.RemoveRange(question.Answers);

            var newAnswers = dto.Answers
                .Where(a => !string.IsNullOrWhiteSpace(a.Content))
                .Select(a => new Answer
                {
                    QuestionId = question.Id,
                    Content = a.Content.Trim(),
                    IsCorrect = a.IsCorrect
                })
                .ToList();

            _context.Answers.AddRange(newAnswers);
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }

        public async Task<OperationResult> DeleteAsync(int questionId, string userId, bool isAdmin)
        {
            if (questionId <= 0)
                return OperationResult.Fail("Nieprawidłowy identyfikator pytania");

            var question = await _context.Questions
                .Include(q => q.Answers)
                .FirstOrDefaultAsync(q => q.Id == questionId);

            if (question == null)
                return OperationResult.Fail("Nie znaleziono pytania");

            // Sprawdzenie uprawnień
            if (!await _quizService.IsOwnerOrAdminAsync(question.QuizId, userId, isAdmin))
                return OperationResult.Fail("Brak uprawnień do usunięcia tego pytania");

            // Usunięcie pytania wraz z odpowiedziami
            _context.Answers.RemoveRange(question.Answers);
            _context.Questions.Remove(question);
            await _context.SaveChangesAsync();

            return OperationResult.Ok();
        }
    }
}
