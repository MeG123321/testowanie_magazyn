using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Magazyn.Data;
using Magazyn.Models;
using System.Net;
using System.Net.Mail;

namespace Magazyn.Controllers;

public class AccountController : Controller
{
    private readonly IWebHostEnvironment _env;
    private readonly ILogger<AccountController> _logger;
    private const int MaxFailedAttempts = 3;
    private const int LockoutMinutes = 15;

    public AccountController(IWebHostEnvironment env, ILogger<AccountController> logger)
    {
        _env = env;
        _logger = logger;
    }

    private string DbPath => Db.GetDbPath(_env);
    private static string SL(string? value) => (value ?? "").Replace('\r', '_').Replace('\n', '_');

    // ==========================================
    // LG_UC1: LOGOWANIE
    // ==========================================
    [HttpGet]
    public IActionResult Login() => View(new LoginViewModel());

    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Login(LoginViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        using var connection = Db.OpenConnection(DbPath);
        UserAuthDto? user = GetUserForAuth(connection, model.Username);

        if (user == null)
        {
            ModelState.AddModelError("", "Niepoprawny login lub hasło");
            return View(model);
        }

        if (user.BlokadaDo.HasValue && user.BlokadaDo.Value > DateTime.Now)
        {
            ModelState.AddModelError("", $"Konto zablokowane do godziny: {user.BlokadaDo.Value:HH:mm}");
            return View(model);
        }

        if (user.Password != model.Password) 
        {
            HandleFailedLogin(connection, user);
            ModelState.AddModelError("", "Niepoprawny login lub hasło");
            return View(model);
        }

        ResetLoginAttempts(connection, user.Id);
        var roles = GetUserRoles(connection, user.Id);
        await SignInUser(user, roles, model.RememberMe);

        if (user.CzyHasloTymczasowe)
            return RedirectToAction("ChangePassword");

        return RedirectToAction("AdminPanel", "Uzytkownicy");
    }

    // ==========================================
    // LG_UC2: WYLOGOWANIE
    // ==========================================
    [HttpPost]
    [ValidateAntiForgeryToken]
    public async Task<IActionResult> Logout()
    {
        await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
        return RedirectToAction("Login");
    }

    // ==========================================
    // LG_UC3: ODZYSKIWANIE HASŁA
    // ==========================================
    [HttpGet]
    public IActionResult RecoverPassword() => View();

[HttpPost]
[ValidateAntiForgeryToken]
public IActionResult RecoverPassword(RecoverPasswordViewModel model)
{
    if (!ModelState.IsValid) return View(model);

    using var connection = Db.OpenConnection(DbPath);
    using var checkCmd = connection.CreateCommand();
    // Pobieramy ID i Email, żeby mieć pewność, dokąd wysłać wiadomość
    checkCmd.CommandText = "SELECT id, Email FROM Uzytkownicy WHERE username = $user AND Email = $email AND czy_zapomniany = 0 LIMIT 1";
    checkCmd.Parameters.AddWithValue("$user", model.Username);
    checkCmd.Parameters.AddWithValue("$email", model.Email);

    using var reader = checkCmd.ExecuteReader();
    if (!reader.Read())
    {
        ModelState.AddModelError("", "Niepoprawne dane. Login lub e-mail są nieprawidłowe.");
        return View(model);
    }

    long userId = Convert.ToInt64(reader["id"]);
    string userEmail = reader["Email"].ToString()!;
    reader.Close(); // Zamykamy reader przed kolejnym zapytaniem

    // Generujemy hasło
    string temporaryPassword = Guid.NewGuid().ToString().Substring(0, 8);

    // Aktualizacja bazy danych
    using var updateCmd = connection.CreateCommand();
    updateCmd.CommandText = "UPDATE Uzytkownicy SET Password = $pass, czy_haslo_tymczasowe = 1, liczba_blednych_logowan = 0, blokada_do = NULL WHERE id = $id";
    updateCmd.Parameters.AddWithValue("$pass", temporaryPassword);
    updateCmd.Parameters.AddWithValue("$id", userId);
    updateCmd.ExecuteNonQuery();

    // --- WYSYŁKA E-MAIL ---
    bool mailSent = SendEmail(userEmail, temporaryPassword);

    if (mailSent)
    {
        TempData["SuccessMessage"] = "Nowe hasło tymczasowe zostało wysłane na Twój adres e-mail.";
    }
    else
    {
        // Jeśli mail nie wyjdzie, pokazujemy hasło na ekranie (opcja ratunkowa)
        TempData["SuccessMessage"] = $"[BŁĄD WYSYŁKI] Twoje hasło tymczasowe to: {temporaryPassword}";
    }

    return View();
}

// Nowa metoda pomocnicza do wysyłki
private bool SendEmail(string targetEmail, string password)
{
    try
    {
        var smtpClient = new SmtpClient("smtp.poczta.pl") // <-- WPISZ SWÓJ HOST SMTP
        {
            Port = 587, // Zazwyczaj 587 lub 465
            Credentials = new NetworkCredential("moj-email@poczta.pl", "moje-haslo-aplikacji"), // <-- TWOJE DANE
            EnableSsl = true,
        };

        var mailMessage = new MailMessage
        {
            From = new MailAddress("moj-email@poczta.pl", "Magazyn GiTA"),
            Subject = "Odzyskiwanie hasła - Hasło tymczasowe",
            Body = $"Witaj!\n\nTwoje nowe hasło tymczasowe do systemu to: {password}\n\nZmień je zaraz po zalogowaniu.",
            IsBodyHtml = false,
        };

        mailMessage.To.Add(targetEmail);
        smtpClient.Send(mailMessage);
        return true;
    }
    catch (Exception ex)
    {
        _logger.LogError(ex, "Błąd podczas wysyłania e-maila do {Email}", targetEmail);
        return false;
    }
}

    // ==========================================
    // LG_UC4: ZMIANA HASŁA (WYMAGANA)
    // ==========================================
    [HttpGet]
    public IActionResult ChangePassword() => View();

    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(ChangePasswordViewModel model)
    {
        if (!ModelState.IsValid) return View(model);

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null) return RedirectToAction("Login");

        using var connection = Db.OpenConnection(DbPath);
        using var cmd = connection.CreateCommand();
        cmd.CommandText = "UPDATE Uzytkownicy SET Password = $pass, czy_haslo_tymczasowe = 0 WHERE id = $id";
        cmd.Parameters.AddWithValue("$pass", model.NewPassword);
        cmd.Parameters.AddWithValue("$id", userIdClaim.Value);
        cmd.ExecuteNonQuery();

        return RedirectToAction("AdminPanel", "Uzytkownicy");
    }

    // ==========================================
    // METODY POMOCNICZE
    // ==========================================
    private UserAuthDto? GetUserForAuth(System.Data.IDbConnection conn, string login)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT id, username, Email, Password, liczba_blednych_logowan, blokada_do, czy_haslo_tymczasowe FROM Uzytkownicy WHERE LOWER(username) = LOWER($login) AND czy_zapomniany = 0 LIMIT 1";
        var p = cmd.CreateParameter(); p.ParameterName = "$login"; p.Value = login.Trim(); cmd.Parameters.Add(p);
        using var r = cmd.ExecuteReader();
        if (!r.Read()) return null;
        return new UserAuthDto {
            Id = Convert.ToInt64(r["id"]),
            Username = r["username"].ToString()!,
            Email = r["Email"].ToString()!,
            Password = r["Password"].ToString()!,
            LiczbaBledow = Convert.ToInt32(r["liczba_blednych_logowan"]),
            BlokadaDo = r["blokada_do"] is DBNull ? null : DateTime.Parse(r["blokada_do"].ToString()!),
            CzyHasloTymczasowe = Convert.ToInt32(r["czy_haslo_tymczasowe"]) == 1
        };
    }

    private void HandleFailedLogin(System.Data.IDbConnection conn, UserAuthDto user)
    {
        int newCount = user.LiczbaBledow + 1;
        object lockoutTime = newCount >= MaxFailedAttempts ? DateTime.Now.AddMinutes(LockoutMinutes).ToString("yyyy-MM-dd HH:mm:ss") : DBNull.Value;
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Uzytkownicy SET liczba_blednych_logowan = $cnt, blokada_do = $lock WHERE id = $id";
        var p1 = cmd.CreateParameter(); p1.ParameterName = "$cnt"; p1.Value = newCount; cmd.Parameters.Add(p1);
        var p2 = cmd.CreateParameter(); p2.ParameterName = "$lock"; p2.Value = lockoutTime; cmd.Parameters.Add(p2);
        var p3 = cmd.CreateParameter(); p3.ParameterName = "$id"; p3.Value = user.Id; cmd.Parameters.Add(p3);
        cmd.ExecuteNonQuery();
    }

    private void ResetLoginAttempts(System.Data.IDbConnection conn, long userId)
    {
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "UPDATE Uzytkownicy SET liczba_blednych_logowan = 0, blokada_do = NULL WHERE id = $id";
        var p = cmd.CreateParameter(); p.ParameterName = "$id"; p.Value = userId; cmd.Parameters.Add(p);
        cmd.ExecuteNonQuery();
    }

    private List<string> GetUserRoles(System.Data.IDbConnection conn, long userId)
    {
        var roles = new List<string>();
        using var cmd = conn.CreateCommand();
        cmd.CommandText = "SELECT p.Nazwa FROM Uprawnienia p JOIN Uzytkownik_Uprawnienia uu ON p.Id = uu.uprawnienie_id WHERE uu.uzytkownik_id = $id";
        var p = cmd.CreateParameter(); p.ParameterName = "$id"; p.Value = userId; cmd.Parameters.Add(p);
        using var r = cmd.ExecuteReader();
        while (r.Read()) roles.Add(r.GetString(0));
        return roles;
    }

    private async Task SignInUser(UserAuthDto user, List<string> roles, bool isPersistent)
    {
        var claims = new List<Claim> {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Username),
            new Claim(ClaimTypes.Email, user.Email)
        };
        foreach (var role in roles) claims.Add(new Claim(ClaimTypes.Role, role));
        var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
        await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, new ClaimsPrincipal(identity), new AuthenticationProperties { IsPersistent = isPersistent });
    }
}

public class UserAuthDto {
    public long Id { get; set; }
    public string Username { get; set; } = "";
    public string Email { get; set; } = "";
    public string Password { get; set; } = "";
    public int LiczbaBledow { get; set; }
    public DateTime? BlokadaDo { get; set; }
    public bool CzyHasloTymczasowe { get; set; }
}