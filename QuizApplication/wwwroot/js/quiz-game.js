/**
 * QuizGameClient - Klasa do obsługi gry quizowej
 * Obsługuje zarówno lobby jak i indywidualną rozgrywkę
 */
class QuizGameClient {
    constructor(code, nickname, isHost) {
        this.code = code;
        this.nickname = nickname;
        this.isHost = isHost;
        this.connection = null;
        this.currentQuestionId = null;
        this.hasAnswered = false;
        this.timerInterval = null;
        this.isInGame = false;
    }

    /**
     * Połącz z serwerem (dla lobby)
     */
    async connect() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/quizHub")
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    if (retryContext.previousRetryCount === 0) return 0;
                    if (retryContext.previousRetryCount === 1) return 2000;
                    if (retryContext.previousRetryCount === 2) return 10000;
                    return 30000;
                }
            })
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.connection.serverTimeoutInMilliseconds = 120000;
        this.connection.keepAliveIntervalInMilliseconds = 15000;

        this.setupEventHandlers();
        this.setupConnectionHandlers();

        try {
            await this.connection.start();
            console.log("SignalR connected");

            if (this.isHost) {
                await this.connection.invoke("JoinGameHost", this.code);
            } else {
                await this.connection.invoke("JoinGamePlayer", this.code, this.nickname);
            }
        } catch (err) {
            console.error("Connection failed:", err);
            this.showError(["Nie udało się połączyć z serwerem gry."]);
        }
    }

    /**
     * Połącz w trybie gry (Play view)
     */
    async connectForGame() {
        this.isInGame = true;

        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/quizHub")
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    if (retryContext.previousRetryCount === 0) return 0;
                    if (retryContext.previousRetryCount === 1) return 2000;
                    if (retryContext.previousRetryCount === 2) return 10000;
                    return 30000;
                }
            })
            .configureLogging(signalR.LogLevel.Warning)
            .build();

        this.connection.serverTimeoutInMilliseconds = 120000;
        this.connection.keepAliveIntervalInMilliseconds = 15000;

        this.setupEventHandlers();
        this.setupConnectionHandlers();

        try {
            await this.connection.start();
            console.log("SignalR connected for game");

            if (this.isHost) {
                await this.connection.invoke("JoinGameHost", this.code);
            } else {
                await this.connection.invoke("JoinGamePlayer", this.code, this.nickname);
                // Poproś o pierwsze pytanie
                setTimeout(() => this.requestNextQuestion(), 300);
            }
        } catch (err) {
            console.error("Connection failed:", err);
            this.showError(["Nie udało się połączyć z serwerem gry."]);
        }
    }

    /**
     * Poproś serwer o następne pytanie
     */
    async requestNextQuestion() {
        try {
            await this.connection.invoke("RequestNextQuestion", this.code);
        } catch (err) {
            console.error("Error requesting next question:", err);
            this.showError(["Błąd pobierania pytania."]);
        }
    }

    /**
     * Konfiguruj event handlery SignalR
     */
    setupEventHandlers() {
        this.connection.on("ShowQuestion", (q) => this.handleQuestion(q));
        this.connection.on("ShowError", (errors) => this.handleError(errors));
        this.connection.on("RevealAnswer", (payload) => this.handleReveal(payload));
        this.connection.on("ScoreboardUpdate", (scoreboard) => this.handleScoreboardUpdate(scoreboard));
        this.connection.on("GameOver", (payload) => this.handleGameOver(payload));
        this.connection.on("UpdatePlayerList", (players) => this.handlePlayerListUpdate(players));
        this.connection.on("GameStarted", () => this.handleGameStarted());
    }

    /**
     * Konfiguruj handlery połączenia
     */
    setupConnectionHandlers() {
        this.connection.onreconnecting(error => {
            console.warn("Connection lost, reconnecting...", error);
        });

        this.connection.onreconnected(async connectionId => {
            console.log("Reconnected with ID:", connectionId);
            try {
                if (this.isHost) {
                    await this.connection.invoke("JoinGameHost", this.code);
                } else {
                    await this.connection.invoke("JoinGamePlayer", this.code, this.nickname);
                    if (this.isInGame) {
                        setTimeout(() => this.requestNextQuestion(), 300);
                    }
                }
            } catch (err) {
                console.error("Rejoin failed:", err);
                this.showError(["Nie udało się ponownie dołączyć do gry."]);
            }
        });

        this.connection.onclose(error => {
            console.error("Connection closed:", error);
            this.showError(["Połączenie zostało zakończone. Odśwież stronę."]);
        });
    }

    /**
     * Obsługa nowego pytania
     */
    handleQuestion(q) {
        console.log("Question received:", q);

        const questionId = q.questionId ?? q.QuestionId;
        const content = q.content ?? q.Content ?? "";
        const answers = q.answers ?? q.Answers ?? [];
        const serverStartUtc = q.serverStartUtc ?? q.ServerStartUtc;
        const timeLimit = q.timeLimitSeconds ?? q.TimeLimitSeconds ?? 30;
        const currentIndex = q.currentQuestionIndex ?? q.CurrentQuestionIndex;
        const totalQuestions = q.totalQuestions ?? q.TotalQuestions;

        this.currentQuestionId = questionId;
        this.hasAnswered = false;

        // Ukryj waiting, pokaż pytanie
        const waitingArea = document.getElementById("waitingArea");
        const questionArea = document.getElementById("questionArea");
        if (waitingArea) waitingArea.style.display = "none";
        if (questionArea) questionArea.style.display = "block";

        // Wyświetl pytanie
        const questionText = document.getElementById("questionText");
        if (questionText) questionText.innerText = content;

        // Wyświetl progress
        const progress = document.getElementById("progress");
        if (progress && currentIndex && totalQuestions) {
            progress.innerText = `Pytanie ${currentIndex} / ${totalQuestions}`;
        }

        // Wyczyść status
        const answerStatus = document.getElementById("answerStatus");
        if (answerStatus) answerStatus.innerText = "";

        // Uruchom timer
        if (serverStartUtc) {
            this.startTimer(serverStartUtc, timeLimit);
        }

        // Renderuj odpowiedzi
        this.renderAnswers(answers);
    }

    /**
     * Renderuj przyciski odpowiedzi
     */
    renderAnswers(answers) {
        const answersDiv = document.getElementById("answers");
        if (!answersDiv) return;

        answersDiv.innerHTML = "";

        answers.forEach(a => {
            const answerId = a.answerId ?? a.AnswerId ?? a.id ?? a.Id;
            const content = a.content ?? a.Content ?? "";

            const btn = document.createElement("button");
            btn.className = "btn btn-outline-primary w-100 mb-2 py-3";
            btn.innerText = content;
            btn.dataset.answerId = answerId;

            if (!this.isHost) {
                btn.onclick = () => this.submitAnswer(answerId);
            } else {
                btn.disabled = true;
            }

            answersDiv.appendChild(btn);
        });
    }

    /**
     * Wyślij odpowiedź
     */
    async submitAnswer(answerId) {
        if (this.hasAnswered) return;

        this.hasAnswered = true;
        this.stopTimer();
        this.disableAllAnswerButtons();

        // Podświetl wybraną odpowiedź
        const btn = document.querySelector(`button[data-answer-id="${answerId}"]`);
        if (btn) {
            btn.classList.remove("btn-outline-primary");
            btn.classList.add("btn-secondary");
        }

        const status = document.getElementById("answerStatus");
        if (status) status.innerText = "Wysyłanie odpowiedzi...";

        try {
            await this.connection.invoke("SendAnswer", this.code, {
                questionId: this.currentQuestionId,
                answerId: answerId
            });
        } catch (err) {
            console.error("Error submitting answer:", err);
            if (status) status.innerText = "Błąd wysyłania odpowiedzi.";
            this.showToast("Błąd wysyłania odpowiedzi", "danger");
        }
    }

    /**
     * Obsługa ujawnienia poprawnej odpowiedzi
     */
    handleReveal(payload) {
        const questionId = payload.questionId ?? payload.QuestionId;
        const correctId = payload.correctAnswerId ?? payload.CorrectAnswerId;

        this.stopTimer();

        // Podświetl poprawną odpowiedź jeśli mamy informację
        if (correctId) {
            document.querySelectorAll("#answers button[data-answer-id]").forEach(btn => {
                const aid = parseInt(btn.dataset.answerId, 10);
                btn.classList.remove("btn-outline-primary", "btn-secondary");
                if (aid === correctId) {
                    btn.classList.add("btn-success");
                } else {
                    btn.classList.add("btn-outline-danger");
                }
                btn.disabled = true;
            });
        }

        const status = document.getElementById("answerStatus");
        if (status) status.innerText = "Odpowiedź zapisana!";

        // Po 1.5 sekundy przejdź do następnego pytania
        setTimeout(() => {
            this.requestNextQuestion();
        }, 1500);
    }

    /**
     * Obsługa aktualizacji scoreboardu
     */
    handleScoreboardUpdate(scoreboard) {
        const list = document.getElementById("scoreboard");
        if (!list) return;

        const players = scoreboard.players ?? scoreboard.Players ?? [];
        list.innerHTML = "";

        players.forEach((p, index) => {
            const name = p.playerName ?? p.PlayerName;
            const score = p.score ?? p.Score;
            const li = document.createElement("li");
            li.className = "list-group-item d-flex justify-content-between align-items-center";

            const medal = index === 0 ? "🥇" : index === 1 ? "🥈" : index === 2 ? "🥉" : "";
            li.innerHTML = `
                <span>${medal} ${name}</span>
                <span class="badge bg-primary rounded-pill">${score}</span>
            `;

            list.appendChild(li);
        });
    }

    /**
     * Obsługa końca gry
     */
    handleGameOver(payload) {
        this.stopTimer();

        // Ukryj pytanie, pokaż wyniki
        const questionArea = document.getElementById("questionArea");
        const waitingArea = document.getElementById("waitingArea");
        const resultsArea = document.getElementById("resultsArea");

        if (questionArea) questionArea.style.display = "none";
        if (waitingArea) waitingArea.style.display = "none";
        if (resultsArea) resultsArea.style.display = "block";

        // Wyświetl scoreboard
        const list = document.getElementById("scoreboard");
        if (list) {
            list.innerHTML = "";

            const players = payload.players ?? payload.Players ?? [];

            if (players.length > 0) {
                players.forEach((p, index) => {
                    const name = p.playerName ?? p.PlayerName;
                    const score = p.score ?? p.Score;
                    const li = document.createElement("li");
                    li.className = "list-group-item d-flex justify-content-between align-items-center";

                    const medal = index === 0 ? "🥇" : index === 1 ? "🥈" : index === 2 ? "🥉" : "";
                    const isMe = name === this.nickname ? " (Ty)" : "";

                    li.innerHTML = `
                        <span>${medal} ${name}${isMe}</span>
                        <span class="badge bg-success rounded-pill">${score} pkt</span>
                    `;

                    list.appendChild(li);
                });
            }
        }

        this.showToast("Koniec quizu! 🎉", "success");
    }

    /**
     * Obsługa błędów
     */
    handleError(errors) {
        const errorText = Array.isArray(errors) ? errors.join("\n") : String(errors);
        console.error("Game error:", errorText);

        if (errorText.includes("nie istnieje") ||
            errorText.includes("zakończył") ||
            errorText.includes("wygasła") ||
            errorText.includes("nie można dołączyć")) {
            alert(errorText);
            window.location.href = "/Game/Join";
        } else {
            this.showToast(errorText, "warning");
        }
    }

    /**
     * Obsługa aktualizacji listy graczy (lobby)
     */
    handlePlayerListUpdate(players) {
        const list = document.getElementById("playersList");
        const countSpan = document.getElementById("playerCount");

        if (list) {
            list.innerHTML = "";

            if (players.length === 0) {
                list.innerHTML = '<li class="list-group-item text-muted fst-italic">Oczekiwanie na graczy...</li>';
            } else {
                players.forEach(name => {
                    const li = document.createElement("li");
                    li.className = "list-group-item";

                    if (name === this.nickname) {
                        li.classList.add("list-group-item-primary");
                        li.textContent = name + " (Ty)";
                    } else {
                        li.textContent = name;
                    }

                    list.appendChild(li);
                });
            }
        }

        if (countSpan) {
            countSpan.textContent = players.length;
        }

        // Dla hosta - włącz przycisk start jeśli są gracze
        const startBtn = document.getElementById("startGameBtn");
        if (startBtn && this.isHost) {
            startBtn.disabled = players.length === 0;
        }
    }

    /**
     * Obsługa rozpoczęcia gry
     */
    handleGameStarted() {
        console.log("Game started!");

        const url = this.isHost
            ? `/Game/PlayHost?code=${encodeURIComponent(this.code)}`
            : `/Game/Play?code=${encodeURIComponent(this.code)}&nick=${encodeURIComponent(this.nickname)}`;

        window.location.href = url;
    }

    /**
     * Uruchom timer
     */
    startTimer(serverStartUtc, limitSec) {
        this.stopTimer();

        const startMs = Date.parse(serverStartUtc);

        const timerEl = document.getElementById("timer");
        if (timerEl) {
            timerEl.classList.remove("text-danger", "text-warning");
        }

        this.timerInterval = setInterval(() => {
            const elapsed = (Date.now() - startMs) / 1000;
            const remaining = Math.max(0, Math.ceil(limitSec - elapsed));

            if (timerEl) {
                timerEl.innerText = remaining;

                if (remaining <= 5) {
                    timerEl.classList.remove("text-warning");
                    timerEl.classList.add("text-danger");
                } else if (remaining <= 10) {
                    timerEl.classList.add("text-warning");
                }
            }

            if (remaining <= 0) {
                this.stopTimer();
                if (!this.hasAnswered) {
                    this.disableAllAnswerButtons();
                    const status = document.getElementById("answerStatus");
                    if (status) status.innerText = "Czas minął!";
                    // Przejdź do następnego pytania
                    setTimeout(() => this.requestNextQuestion(), 1500);
                }
            }
        }, 200);
    }

    /**
     * Zatrzymaj timer
     */
    stopTimer() {
        if (this.timerInterval) {
            clearInterval(this.timerInterval);
            this.timerInterval = null;
        }
    }

    /**
     * Zablokuj wszystkie przyciski odpowiedzi
     */
    disableAllAnswerButtons() {
        document.querySelectorAll("#answers button[data-answer-id]").forEach(btn => {
            btn.disabled = true;
        });
    }

    /**
     * Pokaż toast notification
     */
    showToast(message, type = "info") {
        const toast = document.createElement("div");
        toast.className = `alert alert-${type} position-fixed top-0 start-50 translate-middle-x mt-3`;
        toast.style.zIndex = 9999;
        toast.style.minWidth = "300px";
        toast.textContent = message;

        document.body.appendChild(toast);

        setTimeout(() => {
            toast.style.opacity = "0";
            toast.style.transition = "opacity 0.5s";
            setTimeout(() => toast.remove(), 500);
        }, 3000);
    }

    /**
     * Pokaż błąd
     */
    showError(messages) {
        const text = Array.isArray(messages) ? messages.join("\n") : String(messages);
        this.showToast(text, "danger");
    }
}