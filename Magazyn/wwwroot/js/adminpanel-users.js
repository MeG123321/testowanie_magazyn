document.addEventListener("DOMContentLoaded", () => {
  async function loadUsers(params = {}) {
    const qs = new URLSearchParams();
    if (params.login) qs.set("login", params.login);
    if (params.name) qs.set("name", params.name);
    if (params.pesel) qs.set("pesel", params.pesel);

    const res = await fetch(`/Home/ApiUsers?${qs.toString()}`);
    if (!res.ok) {
      console.error("ApiUsers error:", res.status);
      return;
    }

    const data = await res.json();
    const tbody = document.getElementById("bd-dane");
    if (!tbody) return;

    tbody.innerHTML = "";

    for (const u of data) {
      const tr = document.createElement("tr");

      const cell = (val) => {
        const td = document.createElement("td");
        td.textContent = (val === undefined || val === null || val === "") ? "-" : String(val);
        return td;
      };

      tr.appendChild(cell(u.username));
      tr.appendChild(cell(u.firstName));
      tr.appendChild(cell(u.lastName));
      tr.appendChild(cell(u.email));   
      tr.appendChild(cell(u.pesel));

      const tdA = document.createElement("td");
      const btn = document.createElement("button");
      btn.type = "button";
      btn.className = "btn-primary";
      btn.textContent = "Edytuj";
      tdA.appendChild(btn);
      tr.appendChild(tdA);

      tbody.appendChild(tr);
    }
  }

  loadUsers();

  const form = document.getElementById("emp-search");
  if (form) {
    form.addEventListener("submit", (e) => {
      e.preventDefault();
      loadUsers({
        login: document.getElementById("login")?.value || "",
        name: document.getElementById("name")?.value || "",
        pesel: document.getElementById("pesel")?.value || ""
      });
    });

    form.addEventListener("reset", () => setTimeout(() => loadUsers(), 0));
  }
});