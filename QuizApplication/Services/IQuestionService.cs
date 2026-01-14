using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;
using QuizApplication.DTOs;

namespace QuizApplication.Services
{
    public interface IQuestionService
    {
        Task<OperationResult> AddQuestionAsync(QuestionDto dto, string userId, bool isAdmin);
        Task<OperationResult<QuestionDto>> GetQuestionByIdAsync(int questionId, string userId, bool isAdmin);
        Task<OperationResult> UpdateQuestionAsync(QuestionDto dto, string userId, bool isAdmin);
        Task<OperationResult> DeleteQuestionAsync(int questionId, string userId, bool isAdmin);
    }
}
