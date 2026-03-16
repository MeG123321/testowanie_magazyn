document.addEventListener("DOMContentLoaded", async () => {
    let db = null;

    function setStatus(text) {
        const el = document.getElementById("status-bazy");
        if (el) el.innerText = text;
    }

    function otworzOkno(idOkna) {
        const okno = document.getElementById(idOkna);
        if (okno) okno.style.display = "flex";
    }

    function zamknijWszystkieOkna() {
        document.querySelectorAll(".tlo-okienka").forEach(okno => {
            okno.style.display = "none";
        });
    }

    // URL-e z data-* na <body>
    const body = document.body;
    const adminUrl = body?.dataset?.adminUrl || null; // Index: data-admin-url
    const homeUrl = body?.dataset?.homeUrl || "/";    // AdminPanel: data-home-url

    async function initDatabase() {
        try {
            setStatus("Start initDatabase()...");

            if (typeof initSqlJs !== "function") {
                console.error("initSqlJs is not defined (sql-wasm.js nie wczytany?)");
                setStatus("Błąd: sql.js nie został załadowany");
                return;
            }

            setStatus("Ładowanie SQL.js...");
            const SQL = await initSqlJs({
                locateFile: file => `https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.6.2/${file}`
            });

            setStatus("Pobieranie /magazyn.db ...");
            const res = await fetch("/magazyn.db");
            if (!res.ok) {
                console.error("fetch(/magazyn.db) failed:", res.status);
                setStatus(`Błąd: /magazyn.db HTTP ${res.status} (plik musi być w wwwroot/magazyn.db)`);
                return;
            }

            const buf = await res.arrayBuffer();
            db = new SQL.Database(new Uint8Array(buf));

            // DEBUG: lista tabel
            try {
                const tables = db.exec("SELECT name FROM sqlite_master WHERE type='table' ORDER BY name");
                console.log("TABLES:", tables);
            } catch (e) {
                console.error("Nie mogę odczytać sqlite_master", e);
            }

            // DEBUG: sprawdź czy tabela istnieje
            try {
                const count = db.exec("SELECT COUNT(*) AS cnt FROM UZYTKOWNICY");
                console.log("UZYTKOWNICY COUNT:", count);
            } catch (e) {
                console.error("Tabela UZYTKOWNICY nie istnieje albo ma inną nazwę.", e);
                setStatus("Błąd: nie ma tabeli UZYTKOWNICY w bazie");
                db = null;
                return;
            }

            setStatus("Baza danych aktywna (magazyn.db)");
        } catch (err) {
            console.error("initDatabase error:", err);
            setStatus("Błąd initDatabase(): " + (err?.message || err));
            db = null;
        }
    }

    // Inicjalizacja DB
    await initDatabase();

    // Otwieranie okna logowania
    const btnLogin = document.getElementById("przycisk-zaloguj");
    if (btnLogin) {
        btnLogin.addEventListener("click", (e) => {
            e.preventDefault();
            otworzOkno("okno-logowania");
        });
    }

    // Zamykanie okienek (X)
    document.querySelectorAll(".zamknij-x").forEach(x => {
        x.addEventListener("click", zamknijWszystkieOkna);
    });

    // Zamykanie po kliknięciu w tło
    window.addEventListener("click", (e) => {
        if (e.target && e.target.classList && e.target.classList.contains("tlo-okienka")) {
            zamknijWszystkieOkna();
        }
    });

    // SUBMIT LOGOWANIA (SQLite)
    const formularzLogowania = document.getElementById("formularz-logowania");
    if (formularzLogowania) {
        formularzLogowania.addEventListener("submit", (e) => {
            e.preventDefault();

            const user = (document.getElementById("input-login")?.value || "").trim();
            const pass = (document.getElementById("input-haslo")?.value || "").trim();

            const errorMsg = document.getElementById("error-msg");
            if (errorMsg) errorMsg.style.display = "none";

            if (!db) {
                alert("Baza danych nie jest gotowa lub nie została załadowana.");
                return;
            }

            // DEBUG: pokaż input
            console.log("LOGIN INPUT:", { user, pass });

            // Logowanie login+hasło
            let result = {};
            try {
                const stmt = db.prepare(`
                    SELECT ID, Username, FirstName, LastName
                    FROM UZYTKOWNICY
                    WHERE LOWER(TRIM(Username)) = LOWER(TRIM(:u))
                      AND TRIM(Password) = TRIM(:p)
                    LIMIT 1
                `);

                result = stmt.getAsObject({ ":u": user, ":p": pass });
                stmt.free();
            } catch (e) {
                console.error("Błąd zapytania logowania:", e);
            }

            console.log("LOGIN RESULT:", result);

            // KLUCZOWA POPRAWKA:
            // Nie sprawdzamy result.ID, bo w Twojej bazie ID = NULL.
            // Sprawdzamy czy w ogóle mamy dopasowany rekord (np. Username).
            if (result && result.Username) {
                localStorage.setItem("czyAdmin", "tak");
                alert(`Witaj ${result.FirstName || ""}!`);
                zamknijWszystkieOkna();

                if (!adminUrl) {
                    alert("Brak data-admin-url na <body> w Index.cshtml.");
                    return;
                }

                window.location.href = adminUrl;
            } else {
                if (errorMsg) errorMsg.style.display = "block";
                else alert("Błędne dane!");
            }
        });
    }

    // WYLOGOWANIE (jeśli jest link/przycisk o id=akcja-wyloguj)
    const przyciskWyloguj = document.getElementById("akcja-wyloguj");
    if (przyciskWyloguj) {
        przyciskWyloguj.addEventListener("click", (e) => {
            e.preventDefault();
            localStorage.removeItem("czyAdmin");
            alert("Wylogowano.");
            window.location.href = homeUrl || "/";
        });
    }

    // OCHRONA ADMIN PANELU (demo)
    // Jeśli na body jest data-home-url, uznajemy że to strona admina i wymagamy flagi.
    const jestAdminWidok = !!body?.dataset?.homeUrl;
    if (jestAdminWidok) {
        if (localStorage.getItem("czyAdmin") !== "tak") {
            window.location.href = homeUrl || "/";
            return;
        }
    }
});