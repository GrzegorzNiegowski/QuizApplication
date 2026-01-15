using QuizApplication.DTOs;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;

namespace QuizApplication.Services
{
    public interface IQuestionService
    {
        Task<OperationResult> AddQuestionAsync(CreateQuestionDto dto, string userId, bool isAdmin);
        Task<OperationResult<EditQuestionDto>> GetQuestionForEditAsync(int questionId, string userId, bool isAdmin);
        Task<OperationResult> UpdateQuestionAsync(EditQuestionDto dto, string userId, bool isAdmin);
        Task<OperationResult> DeleteQuestionAsync(int questionId, string userId, bool isAdmin);
    }
}
