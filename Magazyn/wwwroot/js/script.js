document.addEventListener("DOMContentLoaded", () => {
   
    function otworzOkno(idOkna) {
        const okno = document.getElementById(idOkna);
        if(okno) {
            okno.style.display = "flex";
        }
    }
    
    function zamknijWszystkieOkna() {
        document.querySelectorAll('.tlo-okienka').forEach(okno => {
            okno.style.display = "none";
        });
    }
  
    const btnLogin = document.getElementById("przycisk-zaloguj");
    if(btnLogin) {
        btnLogin.addEventListener("click", (e) => {
            e.preventDefault();
            otworzOkno("okno-logowania");
        });
    }

    document.querySelectorAll(".przycisk-edit-post").forEach(btn => {
        btn.addEventListener("click", () => {
            otworzOkno("okno-post");
        });
    });

    document.querySelectorAll(".przycisk-edit-user").forEach(btn => {
        btn.addEventListener("click", () => {
            otworzOkno("okno-user");
        });
    });

    document.querySelectorAll(".zamknij-x").forEach(x => {
        x.addEventListener("click", () => {
            zamknijWszystkieOkna();
        });
    });

    window.addEventListener("click", (e) => {
        if (e.target.classList.contains("tlo-okienka")) {
            zamknijWszystkieOkna();
        }
    });

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

    document.querySelectorAll(".form-edycji").forEach(form => {
        form.addEventListener("submit", (e) => {
            e.preventDefault();
            alert("Dane zostały zaktualizowane!");
            zamknijWszystkieOkna();
        });
    });

    //WYLOGOWANIE DO ZROBIENIA!
    const przyciskWyloguj = document.getElementById("akcja-wyloguj");
    if(przyciskWyloguj) {
        przyciskWyloguj.addEventListener("click", () => {
            localStorage.removeItem("czyAdmin");
            alert("Wylogowano.");
            window.location.href = "~/index.html";
        });
    }

    if(window.location.href.includes("./admin.html")) {
        if(localStorage.getItem("czyAdmin") !== "tak") {
            window.location.href = "./index.html";
        }
    }

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