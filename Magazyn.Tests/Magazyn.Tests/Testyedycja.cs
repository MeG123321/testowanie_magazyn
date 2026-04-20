using Magazyn.Models;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;
using System.Linq;
using Xunit;

namespace Magazyn.Tests.Scenariusze
{
    public class EdycjaUzytkownika_WalidacjaTests
    {
        private UserVm Poprawny()
        {
            return new UserVm
            {
                Id = 3,
                Username = "user03",
                Password = "Test123!", 
                FirstName = "Piotr",
                LastName = "Wiśniewski",
                Email = "piotr.wisniewski@example.com",
                Pesel = "92010978917",
                DataUrodzenia = new DateOnly(1992, 1, 9),
                NrTelefonu = "700800900",
                Plec = "Mężczyzna",
                Status = "Aktywny",
                Miejscowosc = "Poznań",
                KodPocztowy = "60-101",
                NrPosesji = "12A",
                Ulica = "Długa",
                NrLokalu = "5"
            };
        }

        private (bool ok, List<ValidationResult> wyniki) Waliduj(object model)
        {
            var wyniki = new List<ValidationResult>();
            var ctx = new ValidationContext(model);
            var ok = Validator.TryValidateObject(model, ctx, wyniki, validateAllProperties: true);
            return (ok, wyniki);
        }

        private static void AssertHasErrorFor(List<ValidationResult> wyniki, string memberName)
        {
            Assert.Contains(wyniki, w => w.MemberNames.Contains(memberName));
        }

     
        [Fact]
        public void TC_34_Edycja_PustePoleImie()
        {
            var vm = Poprawny();
            vm.FirstName = "";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.FirstName));
        }

        
        [Fact]
        public void TC_35_Edycja_BlednyEmail()
        {
            var vm = Poprawny();
            vm.Email = "piotr.wisniewski.at.example.com";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.Email));
        }

      
        [Fact]
        public void TC_36_Edycja_BlednyTelefon()
        {
            var vm = Poprawny();
            vm.NrTelefonu = "12345678";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.NrTelefonu));
        }

        
        [Fact]
        public void TC_37_Edycja_BlednyKodPocztowy()
        {
            var vm = Poprawny();
            vm.KodPocztowy = "12345";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.KodPocztowy));
        }

      
        [Fact]
        public void TC_38_Edycja_BlednyPesel()
        {
            var vm = Poprawny();
            vm.Pesel = "12345ABCDE1";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.Pesel));
        }
    }
}