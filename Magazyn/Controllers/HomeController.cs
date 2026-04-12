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

    // ============================================
    // DTO: lista (najważniejsze pola)
    // ============================================
    public class UserListRowDto
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Pesel { get; set; }
        public string? Status { get; set; }
        public string? Rola { get; set; }
    }

    // ============================================
    // DTO: szczegóły (pełne)
    // ============================================
    public class UserDetailsDto
    {
        public long Id { get; set; }
        public string? Username { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? Email { get; set; }
        public string? Pesel { get; set; }
        public string? DataUrodzenia { get; set; }
        public string? NrTelefonu { get; set; }
        public string? Plec { get; set; }
        public string? Status { get; set; }
        public string? Rola { get; set; }
        public string? Miejscowosc { get; set; }
        public string? KodPocztowy { get; set; }
        public string? Ulica { get; set; }
        public string? NrPosesji { get; set; }
        public string? NrLokalu { get; set; }
    }

    // ============================================
    // ADMIN PANEL - SERWEROWO (bez JS)
    // ============================================
    [HttpGet]
    public IActionResult AdminPanel(string? login = null, string? name = null, string? pesel = null)
    {
        // żeby trzymało wartości w inputach
        ViewBag.Login = login ?? "";
        ViewBag.Name = name ?? "";
        ViewBag.Pesel = pesel ?? "";

        var results = new List<UserListRowDto>();

        if (!System.IO.File.Exists(DbPath))
            return View(results);

        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
SELECT ID, Username, FirstName, LastName, Pesel, Email, Status, Rola
FROM Uzytkownicy
WHERE COALESCE(Zapomniany,0) = 0
  AND ($login IS NULL OR $login = '' OR LOWER(TRIM(Username)) LIKE '%' || LOWER(TRIM($login)) || '%')
  AND ($name  IS NULL OR $name  = '' OR LOWER(TRIM(FirstName || ' ' || LastName)) LIKE '%' || LOWER(TRIM($name)) || '%')
  AND ($pesel IS NULL OR $pesel = '' OR TRIM(Pesel) LIKE '%' || TRIM($pesel) || '%')
ORDER BY ID;
";
        cmd.Parameters.AddWithValue("$login", login ?? "");
        cmd.Parameters.AddWithValue("$name", name ?? "");
        cmd.Parameters.AddWithValue("$pesel", pesel ?? "");

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            results.Add(new UserListRowDto
            {
                Id = Convert.ToInt64(r["ID"]),
                Username = r["Username"]?.ToString(),
                FirstName = r["FirstName"]?.ToString(),
                LastName = r["LastName"]?.ToString(),
                Email = r["Email"]?.ToString(),
                Pesel = r["Pesel"]?.ToString(),
                Status = r["Status"]?.ToString(),
                Rola = r["Rola"]?.ToString()
            });
        }

        return View(results);
    }

    // ============================================
    // SZCZEGÓŁY USERA - SERWEROWO (bez JS)
    // ============================================
    [HttpGet]
    public IActionResult UserDetails(long id)
    {
        if (!System.IO.File.Exists(DbPath))
            return NotFound(new { msg = "Brak bazy", path = DbPath });

        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
SELECT ID, Username, FirstName, LastName, Pesel, Status, Plec, Rola, DataUrodzenia,
       Email, NrTelefonu,
       Miejscowosc, KodPocztowy, NrPosesji, Ulica, NrLokalu
FROM Uzytkownicy
WHERE ID = $id
LIMIT 1;
";
        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return NotFound(new { msg = "Nie znaleziono użytkownika" });

        var u = new UserDetailsDto
        {
            Id = Convert.ToInt64(r["ID"]),
            Username = r["Username"]?.ToString(),
            FirstName = r["FirstName"]?.ToString(),
            LastName = r["LastName"]?.ToString(),
            Pesel = r["Pesel"]?.ToString(),
            Status = r["Status"]?.ToString(),
            Plec = r["Plec"]?.ToString(),
            Rola = r["Rola"]?.ToString(),
            DataUrodzenia = r["DataUrodzenia"]?.ToString(),
            Email = r["Email"]?.ToString(),
            NrTelefonu = r["NrTelefonu"]?.ToString(),
            Miejscowosc = r["Miejscowosc"]?.ToString(),
            KodPocztowy = r["KodPocztowy"]?.ToString(),
            NrPosesji = r["NrPosesji"]?.ToString(),
            Ulica = r["Ulica"]?.ToString(),
            NrLokalu = r["NrLokalu"]?.ToString(),
        };

        return View(u);
    }

    // ===== RODO widok =====
    [HttpGet]
    public IActionResult ForgottenUsers() => View();

    // =========================
    // REJESTRACJA
    // =========================
    [HttpGet]
    public IActionResult Rejestracja() => View();

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

        // trim
        dto.Username = (dto.Username ?? "").Trim();
        dto.Password = (dto.Password ?? "").Trim();
        dto.FirstName = (dto.FirstName ?? "").Trim();
        dto.LastName = (dto.LastName ?? "").Trim();
        dto.Pesel = (dto.Pesel ?? "").Trim();
        dto.Status = (dto.Status ?? "").Trim();
        dto.Plec = (dto.Plec ?? "").Trim();
        dto.Rola = (dto.Rola ?? "").Trim();
        dto.DataUrodzenia = (dto.DataUrodzenia ?? "").Trim();
        dto.Email = (dto.Email ?? "").Trim();
        dto.NrTelefonu = (dto.NrTelefonu ?? "").Trim();
        dto.Miejscowosc = (dto.Miejscowosc ?? "").Trim();
        dto.KodPocztowy = (dto.KodPocztowy ?? "").Trim();
        dto.NrPosesji = (dto.NrPosesji ?? "").Trim();
        dto.Ulica = (dto.Ulica ?? "").Trim();
        dto.NrLokalu = (dto.NrLokalu ?? "").Trim();

        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        // Username unique
        using (var check = con.CreateCommand())
        {
            check.CommandText = @"SELECT COUNT(*) FROM Uzytkownicy WHERE LOWER(TRIM(Username)) = LOWER(TRIM($u));";
            check.Parameters.AddWithValue("$u", dto.Username);

            var count = Convert.ToInt32(check.ExecuteScalar());
            if (count > 0)
            {
                ModelState.AddModelError("", "Taki login już istnieje.");
                return View(dto);
            }
        }

        // Email unique
        using (var checkEmail = con.CreateCommand())
        {
            checkEmail.CommandText = @"SELECT COUNT(*) FROM Uzytkownicy WHERE LOWER(TRIM(Email)) = LOWER(TRIM($e));";
            checkEmail.Parameters.AddWithValue("$e", dto.Email);

            var count = Convert.ToInt32(checkEmail.ExecuteScalar());
            if (count > 0)
            {
                ModelState.AddModelError("", "Taki email już istnieje.");
                return View(dto);
            }
        }

        // Pesel unique
        using (var checkPesel = con.CreateCommand())
        {
            checkPesel.CommandText = @"SELECT COUNT(*) FROM Uzytkownicy WHERE TRIM(Pesel) = TRIM($p);";
            checkPesel.Parameters.AddWithValue("$p", dto.Pesel);

            var count = Convert.ToInt32(checkPesel.ExecuteScalar());
            if (count > 0)
            {
                ModelState.AddModelError("", "Taki PESEL już istnieje.");
                return View(dto);
            }
        }

        using (var cmd = con.CreateCommand())
        {
            cmd.CommandText = @"
INSERT INTO Uzytkownicy
(Username, Password, FirstName, LastName, Pesel, Status, Plec, Rola, DataUrodzenia, Email, NrTelefonu,
 Miejscowosc, KodPocztowy, NrPosesji, Ulica, NrLokalu,
 ZapomnialUserId, Zapomniany, DataZapomnienia)
VALUES
($Username, $Password, $FirstName, $LastName, $Pesel, $Status, $Plec, $Rola, $DataUrodzenia, $Email, $NrTelefonu,
 $Miejscowosc, $KodPocztowy, $NrPosesji, $Ulica, $NrLokalu,
 NULL, 0, NULL);
";
            cmd.Parameters.AddWithValue("$Username", dto.Username);
            cmd.Parameters.AddWithValue("$Password", dto.Password);
            cmd.Parameters.AddWithValue("$FirstName", dto.FirstName);
            cmd.Parameters.AddWithValue("$LastName", dto.LastName);
            cmd.Parameters.AddWithValue("$Pesel", dto.Pesel);
            cmd.Parameters.AddWithValue("$Status", dto.Status);
            cmd.Parameters.AddWithValue("$Plec", dto.Plec);
            cmd.Parameters.AddWithValue("$Rola", dto.Rola);
            cmd.Parameters.AddWithValue("$DataUrodzenia", dto.DataUrodzenia);
            cmd.Parameters.AddWithValue("$Email", dto.Email);
            cmd.Parameters.AddWithValue("$NrTelefonu", dto.NrTelefonu);
            cmd.Parameters.AddWithValue("$Miejscowosc", dto.Miejscowosc);
            cmd.Parameters.AddWithValue("$KodPocztowy", dto.KodPocztowy);
            cmd.Parameters.AddWithValue("$NrPosesji", dto.NrPosesji);
            cmd.Parameters.AddWithValue("$Ulica", string.IsNullOrWhiteSpace(dto.Ulica) ? DBNull.Value : dto.Ulica);
            cmd.Parameters.AddWithValue("$NrLokalu", string.IsNullOrWhiteSpace(dto.NrLokalu) ? DBNull.Value : dto.NrLokalu);

            cmd.ExecuteNonQuery();
        }

        return RedirectToAction(nameof(AdminPanel));
    }

    // =========================
    // LOGOWANIE
    // =========================
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
FROM Uzytkownicy
WHERE LOWER(TRIM(Username)) = LOWER(TRIM($u))
  AND TRIM(Password) = TRIM($p)
LIMIT 1;
";
        cmd.Parameters.AddWithValue("$u", username.Trim());
        cmd.Parameters.AddWithValue("$p", password.Trim());

        using var reader = cmd.ExecuteReader();
        if (!reader.Read())
            return Unauthorized(new { ok = false, msg = "Błędne dane" });

        return Json(new { ok = true, username = reader["Username"]?.ToString(), rola = reader["Rola"]?.ToString() });
    }

    // =========================
    // API - zostawiamy (może się przydać gdzie indziej)
    // =========================
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
FROM Uzytkownicy
WHERE COALESCE(Zapomniany,0) = 0
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
                pesel = reader["Pesel"]
            });
        }

        return Json(results);
    }

    [HttpGet]
    public IActionResult ApiForgottenUsers(string? name = null, string? adminId = null)
    {
        if (!System.IO.File.Exists(DbPath))
            return NotFound(new { error = "Brak pliku bazy", path = DbPath });

        var results = new List<object>();
        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
SELECT ID,
       FirstName,
       LastName,
       DataZapomnienia,
       ZapomnialUserId
FROM Uzytkownicy
WHERE COALESCE(Zapomniany,0) = 1
  AND ($name IS NULL OR LOWER(TRIM(FirstName || ' ' || LastName)) LIKE '%' || LOWER(TRIM($name)) || '%')
  AND ($adminId IS NULL OR CAST(COALESCE(ZapomnialUserId,0) AS TEXT) LIKE '%' || TRIM($adminId) || '%')
ORDER BY datetime(DataZapomnienia) DESC, ID DESC;
";
        cmd.Parameters.AddWithValue("$name", (object?)name ?? DBNull.Value);
        cmd.Parameters.AddWithValue("$adminId", (object?)adminId ?? DBNull.Value);

        using var r = cmd.ExecuteReader();
        while (r.Read())
        {
            results.Add(new
            {
                id = r["ID"],
                fullNameAfterForget = $"{r["FirstName"]} {r["LastName"]}",
                dataZapomnienia = r["DataZapomnienia"],
                zapomnialUserId = r["ZapomnialUserId"]
            });
        }

        return Json(results);
    }

    [HttpGet]
    public IActionResult ApiUser(long id)
    {
        if (!System.IO.File.Exists(DbPath))
            return NotFound(new { msg = "Brak bazy", path = DbPath });

        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
SELECT ID, Username, Password, FirstName, LastName, Pesel, Status, Plec, Rola, DataUrodzenia,
       Email, NrTelefonu,
       Miejscowosc, KodPocztowy, NrPosesji, Ulica, NrLokalu,
       COALESCE(Zapomniany,0) AS Zapomniany,
       DataZapomnienia,
       ZapomnialUserId
FROM Uzytkownicy
WHERE ID = $id
LIMIT 1;
";
        cmd.Parameters.AddWithValue("$id", id);

        using var r = cmd.ExecuteReader();
        if (!r.Read())
            return NotFound(new { msg = "Nie znaleziono użytkownika" });

        return Json(new
        {
            id = r["ID"],
            username = r["Username"],
            password = r["Password"],
            firstName = r["FirstName"],
            lastName = r["LastName"],
            pesel = r["Pesel"],
            status = r["Status"],
            plec = r["Plec"],
            rola = r["Rola"],
            dataUrodzenia = r["DataUrodzenia"],
            email = r["Email"],
            nrTelefonu = r["NrTelefonu"],
            miejscowosc = r["Miejscowosc"],
            kodPocztowy = r["KodPocztowy"],
            nrPosesji = r["NrPosesji"],
            ulica = r["Ulica"],
            nrLokalu = r["NrLokalu"],
            zapomniany = Convert.ToInt32(r["Zapomniany"]) == 1,
            dataZapomnienia = r["DataZapomnienia"],
            zapomnialUserId = r["ZapomnialUserId"]
        });
    }

    // =========================
    // UPDATE user (edycja) - zostaje jak było
    // =========================
    [HttpPost]
    public IActionResult UpdateUser(UserVm vm)
    {
        if (!ModelState.IsValid)
            return BadRequest(new { msg = "Niepoprawne dane (walidacja)", errors = ModelState });

        if (!System.IO.File.Exists(DbPath))
            return StatusCode(500, new { msg = "Brak bazy", path = DbPath });

        // trim
        vm.Username = (vm.Username ?? "").Trim();
        vm.Password = (vm.Password ?? "").Trim();
        vm.FirstName = (vm.FirstName ?? "").Trim();
        vm.LastName = (vm.LastName ?? "").Trim();
        vm.Pesel = (vm.Pesel ?? "").Trim();
        vm.Status = (vm.Status ?? "").Trim();
        vm.Plec = (vm.Plec ?? "").Trim();
        vm.Rola = (vm.Rola ?? "").Trim();
        vm.DataUrodzenia = (vm.DataUrodzenia ?? "").Trim();
        vm.Email = (vm.Email ?? "").Trim();
        vm.NrTelefonu = (vm.NrTelefonu ?? "").Trim();
        vm.Miejscowosc = (vm.Miejscowosc ?? "").Trim();
        vm.KodPocztowy = (vm.KodPocztowy ?? "").Trim();
        vm.NrPosesji = (vm.NrPosesji ?? "").Trim();
        vm.Ulica = (vm.Ulica ?? "").Trim();
        vm.NrLokalu = (vm.NrLokalu ?? "").Trim();

        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
UPDATE Uzytkownicy
SET Username = $Username,
    Password = $Password,
    FirstName = $FirstName,
    LastName = $LastName,
    Pesel = $Pesel,
    Status = $Status,
    Plec = $Plec,
    Rola = $Rola,
    DataUrodzenia = $DataUrodzenia,
    Email = $Email,
    NrTelefonu = $NrTelefonu,
    Miejscowosc = $Miejscowosc,
    KodPocztowy = $KodPocztowy,
    NrPosesji = $NrPosesji,
    Ulica = $Ulica,
    NrLokalu = $NrLokalu
WHERE ID = $Id;
";
        cmd.Parameters.AddWithValue("$Id", vm.Id);
        cmd.Parameters.AddWithValue("$Username", vm.Username);
        cmd.Parameters.AddWithValue("$Password", vm.Password);
        cmd.Parameters.AddWithValue("$FirstName", vm.FirstName);
        cmd.Parameters.AddWithValue("$LastName", vm.LastName);
        cmd.Parameters.AddWithValue("$Pesel", vm.Pesel);
        cmd.Parameters.AddWithValue("$Status", vm.Status);
        cmd.Parameters.AddWithValue("$Plec", vm.Plec);
        cmd.Parameters.AddWithValue("$Rola", vm.Rola);
        cmd.Parameters.AddWithValue("$DataUrodzenia", vm.DataUrodzenia);
        cmd.Parameters.AddWithValue("$Email", vm.Email);
        cmd.Parameters.AddWithValue("$NrTelefonu", vm.NrTelefonu);
        cmd.Parameters.AddWithValue("$Miejscowosc", vm.Miejscowosc);
        cmd.Parameters.AddWithValue("$KodPocztowy", vm.KodPocztowy);
        cmd.Parameters.AddWithValue("$NrPosesji", vm.NrPosesji);
        cmd.Parameters.AddWithValue("$Ulica", string.IsNullOrWhiteSpace(vm.Ulica) ? DBNull.Value : vm.Ulica);
        cmd.Parameters.AddWithValue("$NrLokalu", string.IsNullOrWhiteSpace(vm.NrLokalu) ? DBNull.Value : vm.NrLokalu);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound(new { msg = "Nie znaleziono użytkownika" });

        return Ok(new { ok = true });
    }

    // =========================
    // RODO: zapomnienie usera (zostaje jak było)
    // =========================
    [HttpPost]
    public IActionResult ForgetUser(long id, long adminId)
    {
        if (!System.IO.File.Exists(DbPath))
            return NotFound(new { msg = "Brak bazy", path = DbPath });

        using var con = new SqliteConnection($"Data Source={DbPath}");
        con.Open();

        static string RandDigits(int len)
        {
            var rng = Random.Shared;
            var chars = new char[len];
            for (int i = 0; i < len; i++) chars[i] = (char)('0' + rng.Next(0, 10));
            return new string(chars);
        }

        static string RandLetters(int len)
        {
            const string a = "abcdefghijklmnopqrstuvwxyz";
            var rng = Random.Shared;
            var chars = new char[len];
            for (int i = 0; i < len; i++) chars[i] = a[rng.Next(a.Length)];
            return char.ToUpper(chars[0]) + new string(chars, 1, len - 1);
        }

        static string RandToken(int len)
        {
            const string a = "abcdefghijklmnopqrstuvwxyz0123456789_";
            var rng = Random.Shared;
            var chars = new char[len];
            for (int i = 0; i < len; i++) chars[i] = a[rng.Next(a.Length)];
            return new string(chars);
        }

        var newFirst = RandLetters(6);
        var newLast = RandLetters(8);
        var newPesel = RandDigits(11);

        var year = Random.Shared.Next(1950, 2006);
        var month = Random.Shared.Next(1, 13);
        var day = Random.Shared.Next(1, DateTime.DaysInMonth(year, month) + 1);
        var newDob = new DateTime(year, month, day).ToString("yyyy-MM-dd");

        var newPlec = Random.Shared.Next(0, 2) == 0 ? "Kobieta" : "Mężczyzna";
        var newStatus = "Nieaktywny";
        var newRola = "Zapomniany";

        var newUsername = "del_" + RandToken(10);
        var newPassword = RandToken(12);
        var newEmail = $"{RandToken(8)}@example.com";

        using var cmd = con.CreateCommand();
        cmd.CommandText = @"
UPDATE Uzytkownicy
SET Zapomniany = 1,
    DataZapomnienia = datetime('now'),
    ZapomnialUserId = $AdminId,

    FirstName = $FirstName,
    LastName = $LastName,
    Pesel = $Pesel,
    DataUrodzenia = $DataUrodzenia,
    Plec = $Plec,

    Status = $Status,
    Rola = $Rola,

    Username = $Username,
    Password = $Password,
    Email = $Email
WHERE ID = $Id;
";
        cmd.Parameters.AddWithValue("$Id", id);
        cmd.Parameters.AddWithValue("$AdminId", adminId);

        cmd.Parameters.AddWithValue("$FirstName", newFirst);
        cmd.Parameters.AddWithValue("$LastName", newLast);
        cmd.Parameters.AddWithValue("$Pesel", newPesel);
        cmd.Parameters.AddWithValue("$DataUrodzenia", newDob);
        cmd.Parameters.AddWithValue("$Plec", newPlec);

        cmd.Parameters.AddWithValue("$Status", newStatus);
        cmd.Parameters.AddWithValue("$Rola", newRola);

        cmd.Parameters.AddWithValue("$Username", newUsername);
        cmd.Parameters.AddWithValue("$Password", newPassword);
        cmd.Parameters.AddWithValue("$Email", newEmail);

        var rows = cmd.ExecuteNonQuery();
        if (rows == 0) return NotFound(new { msg = "Nie znaleziono użytkownika" });

        return Ok(new { ok = true });
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }
    // ============================================
// EDYCJA USERA - SERWEROWO (bez JS)
// ============================================

[HttpGet]
public IActionResult EditUser(long id)
{
    if (!System.IO.File.Exists(DbPath))
        return NotFound(new { msg = "Brak bazy", path = DbPath });

    using var con = new SqliteConnection($"Data Source={DbPath}");
    con.Open();

    using var cmd = con.CreateCommand();
    cmd.CommandText = @"
SELECT ID, Username, Password, FirstName, LastName, Pesel, Status, Plec, Rola, DataUrodzenia,
       Email, NrTelefonu,
       Miejscowosc, KodPocztowy, NrPosesji, Ulica, NrLokalu
FROM Uzytkownicy
WHERE ID = $id
LIMIT 1;
";
    cmd.Parameters.AddWithValue("$id", id);

    using var r = cmd.ExecuteReader();
    if (!r.Read()) return NotFound(new { msg = "Nie znaleziono użytkownika" });

    var vm = new UserVm
    {
        Id = Convert.ToInt64(r["ID"]),
        Username = r["Username"]?.ToString(),
        Password = r["Password"]?.ToString(),
        FirstName = r["FirstName"]?.ToString(),
        LastName = r["LastName"]?.ToString(),
        Pesel = r["Pesel"]?.ToString(),
        Status = r["Status"]?.ToString(),
        Plec = r["Plec"]?.ToString(),
        Rola = r["Rola"]?.ToString(),
        DataUrodzenia = r["DataUrodzenia"]?.ToString(),
        Email = r["Email"]?.ToString(),
        NrTelefonu = r["NrTelefonu"]?.ToString(),
        Miejscowosc = r["Miejscowosc"]?.ToString(),
        KodPocztowy = r["KodPocztowy"]?.ToString(),
        NrPosesji = r["NrPosesji"]?.ToString(),
        Ulica = r["Ulica"]?.ToString(),
        NrLokalu = r["NrLokalu"]?.ToString()
    };

    return View(vm);
}

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult EditUser(UserVm vm)
{
    if (!ModelState.IsValid)
        return View(vm);

    if (!System.IO.File.Exists(DbPath))
    {
        ModelState.AddModelError("", $"Nie znaleziono bazy danych: {DbPath}");
        return View(vm);
    }

    // trim (tak jak w UpdateUser)
    vm.Username = (vm.Username ?? "").Trim();
    vm.Password = (vm.Password ?? "").Trim();
    vm.FirstName = (vm.FirstName ?? "").Trim();
    vm.LastName = (vm.LastName ?? "").Trim();
    vm.Pesel = (vm.Pesel ?? "").Trim();
    vm.Status = (vm.Status ?? "").Trim();
    vm.Plec = (vm.Plec ?? "").Trim();
    vm.Rola = (vm.Rola ?? "").Trim();
    vm.DataUrodzenia = (vm.DataUrodzenia ?? "").Trim();
    vm.Email = (vm.Email ?? "").Trim();
    vm.NrTelefonu = (vm.NrTelefonu ?? "").Trim();
    vm.Miejscowosc = (vm.Miejscowosc ?? "").Trim();
    vm.KodPocztowy = (vm.KodPocztowy ?? "").Trim();
    vm.NrPosesji = (vm.NrPosesji ?? "").Trim();
    vm.Ulica = (vm.Ulica ?? "").Trim();
    vm.NrLokalu = (vm.NrLokalu ?? "").Trim();

    using var con = new SqliteConnection($"Data Source={DbPath}");
    con.Open();

    using var cmd = con.CreateCommand();
    cmd.CommandText = @"
UPDATE Uzytkownicy
SET Username = $Username,
    Password = $Password,
    FirstName = $FirstName,
    LastName = $LastName,
    Pesel = $Pesel,
    Status = $Status,
    Plec = $Plec,
    Rola = $Rola,
    DataUrodzenia = $DataUrodzenia,
    Email = $Email,
    NrTelefonu = $NrTelefonu,
    Miejscowosc = $Miejscowosc,
    KodPocztowy = $KodPocztowy,
    NrPosesji = $NrPosesji,
    Ulica = $Ulica,
    NrLokalu = $NrLokalu
WHERE ID = $Id;
";
    cmd.Parameters.AddWithValue("$Id", vm.Id);
    cmd.Parameters.AddWithValue("$Username", vm.Username);
    cmd.Parameters.AddWithValue("$Password", vm.Password);
    cmd.Parameters.AddWithValue("$FirstName", vm.FirstName);
    cmd.Parameters.AddWithValue("$LastName", vm.LastName);
    cmd.Parameters.AddWithValue("$Pesel", vm.Pesel);
    cmd.Parameters.AddWithValue("$Status", vm.Status);
    cmd.Parameters.AddWithValue("$Plec", vm.Plec);
    cmd.Parameters.AddWithValue("$Rola", vm.Rola);
    cmd.Parameters.AddWithValue("$DataUrodzenia", vm.DataUrodzenia);
    cmd.Parameters.AddWithValue("$Email", vm.Email);
    cmd.Parameters.AddWithValue("$NrTelefonu", vm.NrTelefonu);
    cmd.Parameters.AddWithValue("$Miejscowosc", vm.Miejscowosc);
    cmd.Parameters.AddWithValue("$KodPocztowy", vm.KodPocztowy);
    cmd.Parameters.AddWithValue("$NrPosesji", vm.NrPosesji);
    cmd.Parameters.AddWithValue("$Ulica", string.IsNullOrWhiteSpace(vm.Ulica) ? DBNull.Value : vm.Ulica);
    cmd.Parameters.AddWithValue("$NrLokalu", string.IsNullOrWhiteSpace(vm.NrLokalu) ? DBNull.Value : vm.NrLokalu);

    var rows = cmd.ExecuteNonQuery();
    if (rows == 0)
    {
        ModelState.AddModelError("", "Nie znaleziono użytkownika.");
        return View(vm);
    }

    return RedirectToAction(nameof(UserDetails), new { id = vm.Id });
}
// ============================================
// ZAPOMNIJ z widoku szczegółów (bez JS)
// ============================================
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult ForgetUserFromDetails(long id, long adminId)
{
    // używamy Twojej istniejącej metody ForgetUser (żeby logika była w jednym miejscu)
    var result = ForgetUser(id, adminId);

    // jeśli ok -> wróć do listy personelu
    if (result is OkObjectResult)
        return RedirectToAction(nameof(AdminPanel));

    // jeśli błąd -> pokaż błąd (albo przekieruj do szczegółów)
    return result;
}

// ============================================
// ZMIANA ROLI (popup "Nadaj uprawnienia", bez JS)
// ============================================
[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult SetRole(long id, string rola)
{
    if (string.IsNullOrWhiteSpace(rola))
        return BadRequest(new { msg = "Brak roli" });

    if (!System.IO.File.Exists(DbPath))
        return StatusCode(500, new { msg = "Brak bazy", path = DbPath });

    // prosta walidacja whitelistą ról
    var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase)
    {
        "Admin",
        "Kierownik magazynu",
        "Sprzedawca",
        "Pracownik magazynu"
    };
    if (!allowed.Contains(rola))
        return BadRequest(new { msg = "Nieprawidłowa rola" });

    using var con = new SqliteConnection($"Data Source={DbPath}");
    con.Open();

    using var cmd = con.CreateCommand();
    cmd.CommandText = @"
UPDATE Uzytkownicy
SET Rola = $Rola
WHERE ID = $Id;
";
    cmd.Parameters.AddWithValue("$Id", id);
    cmd.Parameters.AddWithValue("$Rola", rola.Trim());

    var rows = cmd.ExecuteNonQuery();
    if (rows == 0) return NotFound(new { msg = "Nie znaleziono użytkownika" });

    return RedirectToAction(nameof(UserDetails), new { id });
}
}