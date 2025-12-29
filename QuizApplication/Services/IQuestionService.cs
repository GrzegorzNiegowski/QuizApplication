using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;

namespace QuizApplication.Services
{
    public interface IQuestionService
    {
        Task<OperationResult> AddQuestionAsync(AddQuestionViewModel vm, string userId, bool isAdmin);
        // ToDo: Edit/Delete/Fetch methods
        Task<OperationResult<EditQuestionViewModel>> GetQuestionForEditAsync(int questionId, string userId, bool isAdmin);
        Task<OperationResult> EditQuestionAsync(EditQuestionViewModel vm, string userId, bool isAdmin);
        Task<OperationResult> DeleteQuestionAsync(int questionId, string userId, bool isAdmin);
    }
}
