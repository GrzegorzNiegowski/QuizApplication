namespace QuizApplication.Utilities
{
    /// <summary>
    /// Stałe konfiguracyjne gry
    /// </summary>
    public static class GameConstants
    {
        // === Limity graczy ===

        /// <summary>Maksymalna liczba graczy w sesji</summary>
        public const int MaxPlayersPerSession = 100;

        /// <summary>Minimalna liczba graczy do rozpoczęcia gry</summary>
        public const int MinPlayersToStart = 1;

        // === Punktacja ===

        /// <summary>Maksymalna liczba punktów za pytanie (domyślnie)</summary>
        public const int DefaultMaxPoints = 1000;

        /// <summary>Minimalne punkty za poprawną odpowiedź (nawet na ostatnią sekundę)</summary>
        public const int MinPointsForCorrectAnswer = 100;

        // === Czas ===

        /// <summary>Domyślny czas na pytanie (sekundy)</summary>
        public const int DefaultQuestionTimeSeconds = 20;

        /// <summary>Minimalny czas na pytanie (sekundy)</summary>
        public const int MinQuestionTimeSeconds = 5;

        /// <summary>Maksymalny czas na pytanie (sekundy)</summary>
        public const int MaxQuestionTimeSeconds = 300;

        /// <summary>Czas wyświetlania wyników rundy (sekundy)</summary>
        public const int ResultsDisplaySeconds = 3;

        /// <summary>Timeout dla reconnectu gracza (sekundy)</summary>
        public const int PlayerReconnectTimeoutSeconds = 60;

        /// <summary>Timeout dla nieaktywnej sesji w lobby (minuty)</summary>
        public const int SessionLobbyTimeoutMinutes = 30;

        // === Walidacja ===

        /// <summary>Minimalna długość nicku</summary>
        public const int MinNicknameLength = 2;

        /// <summary>Maksymalna długość nicku</summary>
        public const int MaxNicknameLength = 20;

        /// <summary>Długość kodu dostępu</summary>
        public const int AccessCodeLength = 5;

        // === SignalR ===

        /// <summary>Maksymalny rozmiar wiadomości SignalR (bytes)</summary>
        public const long SignalRMaxMessageSizeBytes = 64 * 1024; // 64 KB

        /// <summary>Rozmiar bufora strumienia SignalR</summary>
        public const int SignalRStreamBufferCapacity = 10;

        /// <summary>Interwał batch update "kto odpowiedział" (ms)</summary>
        public const int AnsweredCountUpdateIntervalMs = 500;

        // === Wiadomości ===

        public static class Messages
        {
            public const string GameNotFound = "Nie znaleziono gry o podanym kodzie";
            public const string GameAlreadyStarted = "Gra już się rozpoczęła";
            public const string GameFull = "Gra jest pełna";
            public const string NicknameTaken = "Ten nick jest już zajęty";
            public const string InvalidNickname = "Nieprawidłowy nick";
            public const string NotEnoughPlayers = "Za mało graczy do rozpoczęcia gry";
            public const string HostLeft = "Host zakończył grę";
            public const string PlayerDisconnected = "Gracz się rozłączył";
            public const string QuestionTimeout = "Czas na odpowiedź minął";
            public const string AlreadyAnswered = "Już odpowiedziałeś na to pytanie";
            public const string InvalidAnswer = "Nieprawidłowa odpowiedź";
            public const string NotAuthorized = "Brak uprawnień";
            public const string SessionExpired = "Sesja wygasła";
        }
    }
}
