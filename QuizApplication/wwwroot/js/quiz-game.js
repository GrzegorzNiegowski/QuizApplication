/** 

QuizGameClient - Klasa do obsługi gry quizowej

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
    }
    /**
    
    Połącz z serwerem i dołącz do gry
    */
    async connect() {
        this.connection = new signalR.HubConnectionBuilder()
            .withUrl("/quizHub")
            .withAutomaticReconnect({
                nextRetryDelayInMilliseconds: retryContext => {
                    // Exponential backoff: 0s, 2s, 10s, 30s
                    if (retryContext.previousRetryCount === 0) return 0;
                    if (retryContext.previousRetryCount === 1) return 2000;
                    if (retryContext.previousRetryCount === 2) return 10000;
                    return 30000;
                }
            })
            .configureLogging(signalR.LogLevel.Information)
            .build();

        // Ustaw timeout na kliencie (musi być > niż KeepAliveInterval serwera)
        this.connection.serverTimeoutInMilliseconds = 120000; // 120 sekund
        this.connection.keepAliveIntervalInMilliseconds = 15000; // 15 sekund

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
    
    Konfiguruj event handlery SignalR
    */
    setupEventHandlers() {
        this.connection.on("ShowQuestion", (q) => this.handleQuestion(q));
        this.connection.on("ShowError", (errors) => this.handleError(errors));
        this.connection.on("RevealAnswer", (payload) => this.handleReveal(payload));
        this.connection.on("ScoreboardUpdate", (scoreboard) => this.handleScoreboardUpdate(scoreboard));
        this.connection.on("GameOver", (payload) => this.handleGameOver(payload));
        this.connection.on("AnswerAccepted", () => this.handleAnswerAccepted());
        this.connection.on("UpdatePlayerList", (players) => this.handlePlayerListUpdate(players));
        this.connection.on("GameStarted", () => this.handleGameStarted());
    }

    /**
    
    Konfiguruj handlery połączenia
    */
    setupConnectionHandlers() {
        this.connection.onreconnecting(error => {
            console.warn("Connection lost, reconnecting...", error);
            //this.showToast("Próba ponownego połączenia...", "warning");
        });
        this.connection.onreconnected(async connectionId => {
            console.log("Reconnected with ID:", connectionId);
            //this.showToast("Połączono ponownie", "success");
            // Rejoin do gry
            try {
                if (this.isHost) {
                    await this.connection.invoke("JoinGameHost", this.code);
                } else {
                    await this.connection.invoke("JoinGamePlayer", this.code, this.nickname);
                }
            } catch (err) {
                console.error("Rejoin failed:", err);
            }
        });
        this.connection.onclose(error => {
            console.error("Connection closed:", error);
            this.showError(["Połączenie zostało zakończone. Odśwież stronę."]);
        });
    }

    /**
    
    Obsługa nowego pytania
    */
    handleQuestion(q) {
        console.log("Question received:", q);
        // Normalizuj nazwy pól (C# może zwracać PascalCase lub camelCase)
        const questionId = q.questionId ?? q.QuestionId;
        const content = q.content ?? q.Content ?? "";
        const answers = q.answers ?? q.Answers ?? [];
        const serverStartUtc = q.serverStartUtc ?? q.ServerStartUtc;
        const timeLimit = q.timeLimitSeconds ?? q.TimeLimitSeconds ?? 0;
        const currentIndex = q.currentQuestionIndex ?? q.CurrentQuestionIndex;
        const totalQuestions = q.totalQuestions ?? q.TotalQuestions;
        this.currentQuestionId = questionId;
        this.hasAnswered = false;
        // Ukryj lobby, pokaż pytanie
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
    
    Renderuj przyciski odpowiedzi
    */
    renderAnswers(answers) {
        const answersDiv = document.getElementById("answers");
        if (!answersDiv) return;
        answersDiv.innerHTML = "";
        answers.forEach(a => {
            const answerId = a.answerId ?? a.AnswerId ?? a.id ?? a.Id;
            const content = a.content ?? a.Content ?? "";
            const btn = document.createElement("button");
            btn.className = "btn btn-outline-primary w-100 mb-2";
            btn.innerText = content;
            btn.dataset.answerId = answerId;

            if (!this.isHost) {
                btn.onclick = () => this.submitAnswer(answerId);
            } else {
                btn.disabled = true; // Host nie odpowiada
            }

            answersDiv.appendChild(btn);
        });
    }

    /**
    
    Wyślij odpowiedź
    */
    async submitAnswer(answerId) {
        if (this.hasAnswered) return;
        this.hasAnswered = true;
        this.disableAllAnswerButtons();
        // Podświetl wybraną odpowiedź
        const btn = document.querySelector(`button[data-answer-id="${answerId}"]`);
        if (btn) {
            btn.classList.remove("btn-outline-primary");
            btn.classList.add("btn-secondary");
        }
        try {
            await this.connection.invoke("SendAnswer", this.code, {
                questionId: this.currentQuestionId,
                answerId: answerId
            });
            const status = document.getElementById("answerStatus");
            if (status) status.innerText = "Wysłano odpowiedź...";
        } catch (err) {
            console.error("Error submitting answer:", err);
            this.hasAnswered = false;
            const status = document.getElementById("answerStatus");
            if (status) status.innerText = "Błąd wysyłania odpowiedzi.";

            this.showToast("Błąd wysyłania odpowiedzi", "danger");
        }
    }

    /**
    
    Obsługa zaakceptowania odpowiedzi
    */
    handleAnswerAccepted() {
        const status = document.getElementById("answerStatus");
        if (status) status.innerText = "Odpowiedź przyjęta ✅";
    }

    /**
    
    Obsługa ujawnienia poprawnej odpowiedzi
    */
    handleReveal(payload) {
        const questionId = payload.questionId ?? payload.QuestionId;
        const correctId = payload.correctAnswerId ?? payload.CorrectAnswerId;
        // Ignoruj jeśli to nie aktualne pytanie
        if (this.currentQuestionId && questionId && this.currentQuestionId !== questionId) {
            return;
        }
        this.stopTimer();
        // Podświetl poprawną i błędne odpowiedzi
        document.querySelectorAll("#answers button[data-answer-id]").forEach(btn => {
            const answerId = parseInt(btn.dataset.answerId, 10);
            btn.classList.remove("btn-outline-primary", "btn-secondary");
            if (answerId === correctId) {
                btn.classList.add("btn-success");
            } else {
                btn.classList.add("btn-outline-danger");
            }

            btn.disabled = true;
        });
        const status = document.getElementById("answerStatus");
        if (status) status.innerText = "Koniec pytania.";
        // Dla hosta - pokaż w reveal
        const reveal = document.getElementById("reveal");
        if (reveal && this.isHost) {
            reveal.innerText = `Poprawna odpowiedź ID: ${correctId ?? "-"}`;
        }
    }

    /**
    
    Obsługa aktualizacji scoreboardu
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
    
    Obsługa końca gry
    */
    handleGameOver(payload) {
        this.stopTimer();
        const list = document.getElementById("scoreboard");
        if (list) list.innerHTML = "";
        // Payload może być dictionary lub ScoreboardDto
        if (payload && (payload.players || payload.Players)) {
            const players = payload.players ?? payload.Players;
            players.forEach((p, index) => {
                const name = p.playerName ?? p.PlayerName;
                const score = p.score ?? p.Score;
                const li = document.createElement("li");
                li.className = "list-group-item d-flex justify-content-between align-items-center";

                const medal = index === 0 ? "🥇" : index === 1 ? "🥈" : index === 2 ? "🥉" : "";
                li.innerHTML = `
         <span>${medal} ${name}</span>
         <span class="badge bg-success rounded-pill">${score}</span>
         `;

                list.appendChild(li);
            });
        } else if (payload && typeof payload === "object") {
            // Dictionary nick->score
            const sorted = Object.entries(payload).sort((a, b) => b[1] - a[1]);
            sorted.forEach(([name, score], index) => {
                const li = document.createElement("li");
                li.className = "list-group-item d-flex justify-content-between align-items-center";

                const medal = index === 0 ? "🥇" : index === 1 ? "🥈" : index === 2 ? "🥉" : "";
                li.innerHTML = `
                <span>${medal} ${name}</span>
                <span class="badge bg-success rounded-pill">${score}</span>
            `;

                list.appendChild(li);
            });
        }

        this.showToast("Koniec gry! 🎉", "success");

        setTimeout(() => {
            alert("Dziękujemy za grę!");
        }, 1000);
    }

    /**
     * Obsługa błędów
     */
    handleError(errors) {
        const errorText = Array.isArray(errors) ? errors.join("\n") : String(errors);
        console.error("Game error:", errorText);

        // Krytyczne błędy = przekierowanie
        if (errorText.includes("nie istnieje") ||
            errorText.includes("zakończył") ||
            errorText.includes("wygasła")) {
            alert(errorText);
            window.location.href = "/Game/Join";
        } else {
            // Mniejsze błędy = toast
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

        // Przekieruj do widoku gry
        const url = this.isHost
            ? `/Game/PlayHost?code=${encodeURIComponent(this.code)}`
            : `/Game/Play?code=${encodeURIComponent(this.code)}&nick=${encodeURIComponent(this.nickname)}`;

        window.location.href = url;
    }

    /**
     * Uruchom timer
     */
    startTimer(serverStartUtc, limitSec) {
        this.stopTimer(); // Stop previous timer if any

        const startMs = Date.parse(serverStartUtc);

        this.timerInterval = setInterval(() => {
            const elapsed = (Date.now() - startMs) / 1000;
            const remaining = Math.max(0, Math.ceil(limitSec - elapsed));

            const timerEl = document.getElementById("timer");
            if (timerEl) {
                timerEl.innerText = remaining;

                // Zmień kolor gdy mało czasu
                if (remaining <= 5) {
                    timerEl.classList.add("text-danger");
                } else if (remaining <= 10) {
                    timerEl.classList.add("text-warning");
                }
            }

            if (remaining <= 0) {
                this.stopTimer();
                this.disableAllAnswerButtons();
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
     * Pokaż błąd (alias dla showToast)
     */
    showError(messages) {
        const text = Array.isArray(messages) ? messages.join("\n") : String(messages);
        this.showToast(text, "danger");
    }
}