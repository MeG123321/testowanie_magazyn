document.addEventListener("DOMContentLoaded", () => {
    
    // 1. OBSŁUGA OKIEN (POP-UP) 
    
    // Funkcja otwierająca konkretne okno
    function otworzOkno(idOkna) {
        const okno = document.getElementById(idOkna);
        if(okno) {
            okno.style.display = "flex";
        }
    }

    // Funkcja zamykająca wszystkie okna
    function zamknijWszystkieOkna() {
        document.querySelectorAll('.tlo-okienka').forEach(okno => {
            okno.style.display = "none";
        });
    }

    // Otwieranie logowania (index.html)
    const btnLogin = document.getElementById("przycisk-zaloguj");
    if(btnLogin) {
        btnLogin.addEventListener("click", (e) => {
            e.preventDefault();
            otworzOkno("okno-logowania");
        });
    }

    // Otwieranie edycji POSTU (admin.html)
    document.querySelectorAll(".przycisk-edit-post").forEach(btn => {
        btn.addEventListener("click", () => {
            otworzOkno("okno-post");
        });
    });

    // Otwieranie edycji UŻYTKOWNIKA (admin.html)
    document.querySelectorAll(".przycisk-edit-user").forEach(btn => {
        btn.addEventListener("click", () => {
            otworzOkno("okno-user");
        });
    });

    // Zamykanie krzyżykiem (X)
    document.querySelectorAll(".zamknij-x").forEach(x => {
        x.addEventListener("click", () => {
            zamknijWszystkieOkna();
        });
    });

    // Zamykanie kliknięciem w ciemne tło
    window.addEventListener("click", (e) => {
        if (e.target.classList.contains("tlo-okienka")) {
            zamknijWszystkieOkna();
        }
    });

    // 2. FORMULARZE

    // Logowanie
    const formularzLogowania = document.getElementById("formularz-logowania");
    if(formularzLogowania) {
        formularzLogowania.addEventListener("submit", (e) => {
            e.preventDefault();
            const loginInput = document.getElementById("input-login").value;
            const hasloInput = document.getElementById("input-haslo").value;

            if(loginInput === "admin" && hasloInput === "admin") {
                localStorage.setItem("czyAdmin", "tak");
                alert("Zalogowano pomyślnie!");
                window.location.href = "~/adminpanel.html";
            } else {
                alert("Błąd! Hasło lub login jest błędny");
            }
        });
    }

    // Edycja (admin.html)
    document.querySelectorAll(".form-edycji").forEach(form => {
        form.addEventListener("submit", (e) => {
            e.preventDefault();
            alert("Dane zostały zaktualizowane!");
            zamknijWszystkieOkna();
        });
    });

    // 3. WYLOGOWANIE 
    const przyciskWyloguj = document.getElementById("akcja-wyloguj");
    if(przyciskWyloguj) {
        przyciskWyloguj.addEventListener("click", () => {
            localStorage.removeItem("czyAdmin");
            alert("Wylogowano.");
            window.location.href = "~/index.html";
        });
    }

    // 4. ZABEZPIECZENIE PANELU 
    if(window.location.href.includes("./admin.html")) {
        if(localStorage.getItem("czyAdmin") !== "tak") {
            window.location.href = "./index.html";
        }
    }

    // 5. SLIDER (PĘTLA) 
    const slider = document.getElementById("moj-slider");
    const btnLewo = document.getElementById("przewin-lewo");
    const btnPrawo = document.getElementById("przewin-prawo");

    if(slider && btnLewo && btnPrawo) {
        
        const szerokoscPrzesuwu = 150; 

        btnPrawo.addEventListener("click", () => {
            const maxScroll = slider.scrollWidth - slider.clientWidth;
            if (slider.scrollLeft >= maxScroll - 10) {
                slider.scrollTo({ left: 0, behavior: 'smooth' });
            } else {
                slider.scrollBy({ left: szerokoscPrzesuwu, behavior: 'smooth' });
            }
        });

        btnLewo.addEventListener("click", () => {
            if (slider.scrollLeft <= 10) {
                slider.scrollTo({ left: slider.scrollWidth, behavior: 'smooth' });
            } else {
                slider.scrollBy({ left: -szerokoscPrzesuwu, behavior: 'smooth' });
            }
        });
    }
});