namespace QuizApplication.Utilities
{
    public class SessionCodeHelper
    // Normalizuje kod sesji do standardowej formy
    {
        public static string Normalize(string? code)
        {
            return (code ?? "").Trim().ToUpperInvariant();
        }

        /// <summary>
        /// Sprawdza czy kod sesji jest poprawny
        /// </summary>
        public static bool IsValid(string? code)
        {
            var normalized = Normalize(code);

            if (normalized.Length < GameConstants.MinSessionCodeLength ||normalized.Length > GameConstants.MaxSessionCodeLength)
            {
                return false;
            }

            return normalized.All(c => char.IsLetterOrDigit(c));
        }
    }
}
