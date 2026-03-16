using System.ComponentModel.DataAnnotations;

namespace Magazyn.Models
{
    public class UserRegistrationDto
    {
        [Required, StringLength(20, MinimumLength = 5)]
        public string Username { get; set; } = "";

        [Required, StringLength(30, MinimumLength = 4)]
        public string Password { get; set; } = "";

        [Required]
        public string FirstName { get; set; } = "";

        [Required]
        public string LastName { get; set; } = "";

        [Required]
        public string Adres { get; set; } = "";

        [Required, RegularExpression(@"^\d{11}$")]
        public string Pesel { get; set; } = "";

        [Required, RegularExpression(@"^\d{9}$")]
        public string NrTelefonu { get; set; } = "";

        [Required]
        public string Plec { get; set; } = ""; // "Kobieta"/"Mężczyzna"

        [Required]
        public string Status { get; set; } = "Aktywny";

        [Required]
        public string Rola { get; set; } = "Użytkownik";
        [Required, EmailAddress]
        public string Email { get; set; } = "";
    }
}