using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;

namespace QuizApplication.Services
{
    public interface IQuestionService
    {
        Task<OperationResult> AddQuestionAsync(AddQuestionViewModel vm, string userId, bool isAdmin);
        // ToDo: Edit/Delete/Fetch methods
    }
}
