document.addEventListener("DOMContentLoaded", () => {
    let db = null;

    const body = document.body;
    const adminUrl = body?.dataset?.adminUrl || null; // Index.cshtml
    const homeUrl = body?.dataset?.homeUrl || "/";    // AdminPanel.cshtml

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

    async function initDatabase() {
        try {
            setStatus("Ładowanie bazy danych...");

            if (typeof initSqlJs !== "function") {
                console.error("initSqlJs is not defined - czy sql-wasm.js się wczytał?");
                setStatus("Błąd: sql.js nie został załadowany");
                return null;
            }

            const SQL = await initSqlJs({
                locateFile: file => `https://cdnjs.cloudflare.com/ajax/libs/sql.js/1.6.2/${file}`
            });

            const res = await fetch("/magazyn.db");
            if (!res.ok) {
                console.error("Nie można pobrać /magazyn.db:", res.status);
                setStatus(`Błąd: /magazyn.db HTTP ${res.status}`);
                return null;
            }

            const buf = await res.arrayBuffer();
            const database = new SQL.Database(new Uint8Array(buf));

            setStatus("Baza danych aktywna (magazyn.db)");
            return database;
        } catch (e) {
            console.error("initDatabase error:", e);
            setStatus("Błąd initDatabase(): " + (e?.message || e));
            return null;
        }
    }

    function renderUsersTable(database, filters = {}) {
        const tbody = document.getElementById("bd-dane");
        if (!tbody) return;

        const login = (filters.login || "").trim().toLowerCase();
        const name = (filters.name || "").trim().toLowerCase();
        const pesel = (filters.pesel || "").trim();

        const stmt = database.prepare(`
            SELECT
              ID,
              Username,
              Password,
              FirstName,
              LastName,
              Adres,
              Pesel,
              Status,
              Plec,
              Rola,
              NrTelefonu
            FROM UZYTKOWNICY
            WHERE 1=1
              AND (:login = '' OR LOWER(TRIM(Username)) LIKE '%' || :login || '%')
              AND (:name  = '' OR LOWER(TRIM(FirstName || ' ' || LastName)) LIKE '%' || :name || '%')
              AND (:pesel = '' OR TRIM(Pesel) LIKE '%' || :pesel || '%')
            ORDER BY ID
        `);

        stmt.bind({ ":login": login, ":name": name, ":pesel": pesel });

        tbody.innerHTML = "";

        while (stmt.step()) {
            const row = stmt.getAsObject();
            const tr = document.createElement("tr");

            const makeTd = (v) => {
                const td = document.createElement("td");
                td.textContent = (v === undefined || v === null || v === "") ? "-" : v.toString();
                return td;
            };

            tr.appendChild(makeTd(row.ID));
            tr.appendChild(makeTd(row.Username));
            tr.appendChild(makeTd(row.Password));
            tr.appendChild(makeTd(row.FirstName));
            tr.appendChild(makeTd(row.LastName));
            tr.appendChild(makeTd(row.Adres));
            tr.appendChild(makeTd(row.Pesel));
            tr.appendChild(makeTd(row.Status));
            tr.appendChild(makeTd(row.Plec));
            tr.appendChild(makeTd(row.Rola));
            tr.appendChild(makeTd(row.NrTelefonu));

            const tdAkcje = document.createElement("td");
            const btn = document.createElement("button");
            btn.type = "button";
            btn.className = "btn-primary";
            btn.textContent = "Edytuj";
            tdAkcje.appendChild(btn);
            tr.appendChild(tdAkcje);

            tbody.appendChild(tr);
        }

        stmt.free();
    }

    (async () => {
        db = await initDatabase();

        // ===== INDEX: okno logowania =====
        const btnLogin = document.getElementById("przycisk-zaloguj");
        if (btnLogin) {
            btnLogin.addEventListener("click", (e) => {
                e.preventDefault();
                otworzOkno("okno-logowania");
            });
        }

        const closeX = document.getElementById("zamknij-logowanie");
        if (closeX) closeX.addEventListener("click", zamknijWszystkieOkna);

        window.addEventListener("click", (e) => {
            if (e.target && e.target.classList && e.target.classList.contains("tlo-okienka")) {
                zamknijWszystkieOkna();
            }
        });

        // ===== LOGOWANIE =====
        const loginForm = document.getElementById("formularz-logowania");
        if (loginForm) {
            loginForm.addEventListener("submit", (e) => {
                e.preventDefault();

                const user = (document.getElementById("input-login")?.value || "").trim();
                const pass = (document.getElementById("input-haslo")?.value || "").trim();

                const errorMsg = document.getElementById("error-msg");
                if (errorMsg) errorMsg.style.display = "none";

                if (!db) {
                    alert("Baza danych nie jest gotowa.");
                    return;
                }

                const stmt = db.prepare(`
                    SELECT Username, FirstName, LastName, Rola
                    FROM UZYTKOWNICY
                    WHERE LOWER(TRIM(Username)) = LOWER(TRIM(:u))
                      AND TRIM(Password) = TRIM(:p)
                    LIMIT 1
                `);

                const result = stmt.getAsObject({ ":u": user, ":p": pass });
                stmt.free();

                if (result && result.Username) {
                    localStorage.setItem("czyAdmin", "tak");
                    localStorage.setItem("rola", result.Rola || "");
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

        // ===== ADMIN PANEL =====
        const isAdminView = !!body?.dataset?.homeUrl;
        if (isAdminView) {
            if (localStorage.getItem("czyAdmin") !== "tak") {
                window.location.href = homeUrl || "/";
                return;
            }

            if (db) {
                renderUsersTable(db);
            }

            const searchForm = document.getElementById("emp-search");
            if (searchForm) {
                searchForm.addEventListener("submit", (e) => {
                    e.preventDefault();
                    if (!db) return;

                    const login = document.getElementById("login")?.value || "";
                    const name = document.getElementById("name")?.value || "";
                    const pesel = document.getElementById("pesel")?.value || "";

                    renderUsersTable(db, { login, name, pesel });
                });

                searchForm.addEventListener("reset", () => {
                    if (!db) return;
                    setTimeout(() => renderUsersTable(db), 0);
                });
            }

            const logoutBtn = document.getElementById("akcja-wyloguj");
            if (logoutBtn) {
                logoutBtn.addEventListener("click", (e) => {
                    e.preventDefault();
                    localStorage.removeItem("czyAdmin");
                    localStorage.removeItem("rola");
                    window.location.href = homeUrl || "/";
                });
            }
        }
    })();
});