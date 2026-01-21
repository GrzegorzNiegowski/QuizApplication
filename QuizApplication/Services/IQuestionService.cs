using QuizApplication.DTOs;
using QuizApplication.DTOs.QuestionDtos;
using QuizApplication.Models.ViewModels;
using QuizApplication.Utilities;

namespace QuizApplication.Services
{
    public interface IQuestionService
    {
        /// <summary>
        /// Interfejs serwisu zarządzającego pytaniami w quizach
        /// </summary>
        public interface IQuestionService
        {
            /// <summary>
            /// Dodaje nowe pytanie wraz z odpowiedziami do quizu
            /// </summary>
            Task<OperationResult> AddQuestionAsync(CreateQuestionDto dto, string userId, bool isAdmin);

            /// <summary>
            /// Pobiera dane pytania do formularza edycji
            /// </summary>
            Task<OperationResult<EditQuestionDto>> GetQuestionForEditAsync(int questionId, string userId, bool isAdmin);

            /// <summary>
            /// Aktualizuje pytanie wraz z odpowiedziami
            /// </summary>
            Task<OperationResult> UpdateQuestionAsync(EditQuestionDto dto, string userId, bool isAdmin);

            /// <summary>
            /// Usuwa pytanie wraz ze wszystkimi odpowiedziami
            /// </summary>
            Task<OperationResult> DeleteQuestionAsync(int questionId, string userId, bool isAdmin);
        }
}
