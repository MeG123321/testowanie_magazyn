using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Data.Sqlite;
using Magazyn.Models;

namespace Magazyn.Controllers;

public class HomeController : Controller
{
    private readonly IWebHostEnvironment _env;

    public HomeController(IWebHostEnvironment env)
    {
        _env = env;
    }

    private string DbPath => Path.Combine(_env.WebRootPath, "magazyn.db");

    public IActionResult Index() => View();

    public IActionResult Privacy() => View();

    public IActionResult AdminPanel() => View();

    [HttpGet]
    public IActionResult Rejestracja() => View();

    // REJESTRACJA (POST) - zapis do sqlite na serwerze
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult Rejestracja(UserRegistrationDto dto)
    {
        if (!ModelState.IsValid)
            return View(dto);

        if (!System.IO.File.Exists(DbPath))
        {
            ModelState.AddModelError("", $"Nie znaleziono bazy danych: {DbPath}");
            return View(dto);
        }

        // Trim/bezpieczne wartości
        dto.Username = (dto.Username ?? "").Trim();
        dto.Password = (dto.Password ?? "").Trim();
        dto.FirstName = (dto.FirstName ?? "").Trim();
        dto.LastName = (dto.LastName ?? "").Trim();
        dto.Adres = (dto.Adres ?? "").Trim();
        dto.Pesel = (dto.Pesel ?? "").Trim();
        dto.Status = (dto.Status ?? "").Trim();
        dto.Plec = (dto.Plec ?? "").Trim();
        dto.Rola = (dto.Rola ?? "").Trim();
        dto.NrTelefonu = (dto.NrTelefonu ?? "").Trim();
        dto.Email = (dto.Email ?? "").Trim();

        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        // Login unikalny
        using (var check = con.CreateCommand())
        {
            check.CommandText = @"
SELECT COUNT(*)
FROM UZYTKOWNICY
WHERE LOWER(TRIM(Username)) = LOWER(TRIM($u));
";
            check.Parameters.AddWithValue("$u", dto.Username);

            var countObj = check.ExecuteScalar();
            var count = Convert.ToInt32(countObj);

            if (count > 0)
            {
                ModelState.AddModelError("", "Taki login już istnieje.");
                return View(dto);
            }
        }

        // Insert (z Email)
        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = @"
INSERT INTO UZYTKOWNICY
(Username, Password, FirstName, LastName, Adres, Pesel, Status, Plec, Rola, NrTelefonu, Email)
VALUES
($Username, $Password, $FirstName, $LastName, $Adres, $Pesel, $Status, $Plec, $Rola, $NrTelefonu, $Email);
";
            cmd.Parameters.AddWithValue("$Username", dto.Username);
            cmd.Parameters.AddWithValue("$Password", dto.Password);
            cmd.Parameters.AddWithValue("$FirstName", dto.FirstName);
            cmd.Parameters.AddWithValue("$LastName", dto.LastName);
            cmd.Parameters.AddWithValue("$Adres", dto.Adres);
            cmd.Parameters.AddWithValue("$Pesel", dto.Pesel);
            cmd.Parameters.AddWithValue("$Status", dto.Status);
            cmd.Parameters.AddWithValue("$Plec", dto.Plec);
            cmd.Parameters.AddWithValue("$Rola", dto.Rola);
            cmd.Parameters.AddWithValue("$NrTelefonu", dto.NrTelefonu);
            cmd.Parameters.AddWithValue("$Email", dto.Email);

            cmd.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(AdminPanel));
    }

    // LOGOWANIE (backend) - POST /Home/Login
    [HttpPost]
    public IActionResult Login(string username, string password)
    {
        if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(password))
            return BadRequest(new { ok = false, msg = "Brak loginu lub hasła" });

        if (!System.IO.File.Exists(DbPath))
            return StatusCode(500, new { ok = false, msg = "Brak bazy", path = DbPath });

        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
SELECT Username, Rola
FROM UZYTKOWNICY
WHERE LOWER(TRIM(Username)) = LOWER(TRIM($u))
  AND TRIM(Password) = TRIM($p)
LIMIT 1;
";
        cmd.Parameters.AddWithValue("$u", username.Trim());
        cmd.Parameters.AddWithValue("$p", password.Trim());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return Unauthorized(new { ok = false, msg = "Błędne dane" });

        return Json(new
        {
            ok = true,
            username = reader["Username"]?.ToString(),
            rola = reader["Rola"]?.ToString()
        });
    }

    // API lista użytkowników do AdminPanel
    [HttpGet]
    public IActionResult ApiUsers(string? login = null, string? name = null, string? pesel = null)
    {
        if (!System.IO.File.Exists(DbPath))
            return NotFound(new { error = "Brak pliku bazy", path = DbPath });

        var results = new List<object>();

        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
SELECT ID, Username, FirstName, LastName, Pesel, Email
FROM UZYTKOWNICY
WHERE 1=1
  AND ($login IS NULL OR LOWER(TRIM(Username)) LIKE '%' || LOWER(TRIM($login)) || '%')
  AND ($name  IS NULL OR LOWER(TRIM(FirstName || ' ' || LastName)) LIKE '%' || LOWER(TRIM($name)) || '%')
  AND ($pesel IS NULL OR TRIM(Pesel) LIKE '%' || TRIM($pesel) || '%')
ORDER BY ID;
";
        cmd.Parameters.AddWithValue("$login", (object?)login ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$name", (object?)name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$pesel", (object?)pesel ?? DBNull.Value);

        using var reader = cmd.ExecuteReader();
        while (reader.Read())
        {
            results.Add(new
            {
                id = reader["ID"],
                username = reader["Username"],
                firstName = reader["FirstName"],
                lastName = reader["LastName"],
                email = reader["Email"],
                pesel = reader["Pesel"],
            });
        }

        return Json(results);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
}