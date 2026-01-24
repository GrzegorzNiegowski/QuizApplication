using QuizApplication.DTOs;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;

namespace QuizApplication.Services
{
    
        /// <summary>
        /// Serwis do zarządzania pytaniami
        /// </summary>
        public interface IQuestionService
        {
            /// <summary>
            /// Dodaje nowe pytanie do quizu
            /// </summary>
            Task<OperationResult> CreateAsync(CreateQuestionDto dto, string userId, bool isAdmin);

            /// <summary>
            /// Pobiera pytanie do edycji
            /// </summary>
            Task<OperationResult<EditQuestionDto>> GetForEditAsync(int questionId, string userId, bool isAdmin);

            /// <summary>
            /// Aktualizuje pytanie
            /// </summary>
            Task<OperationResult> UpdateAsync(EditQuestionDto dto, string userId, bool isAdmin);

            /// <summary>
            /// Usuwa pytanie
            /// </summary>
            Task<OperationResult> DeleteAsync(int questionId, string userId, bool isAdmin);
        }
}
