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

        [Fact]
        public void TC_71_Edycja_Email_BezMalpy_Blad()
        {
            var vm = Poprawny();
            vm.Email = "test.pl";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.Email));
        }

        [Fact]
        public void TC_72_Edycja_Login_ZaKrotki_Blad()
        {
            var vm = Poprawny();
            vm.Username = "abc";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.Username));
        }

        [Fact]
        public void TC_73_Edycja_Telefon_Litery_Blad()
        {
            var vm = Poprawny();
            vm.NrTelefonu = "123abc789";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.NrTelefonu));
        }

        [Fact]
        public void TC_74_Edycja_Pesel_Litery_Blad()
        {
            var vm = Poprawny();
            vm.Pesel = "85010A12345";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.Pesel));
        }

        [Fact]
        public void TC_75_Edycja_KodPocztowy_BrakMyslnika_Blad()
        {
            var vm = Poprawny();
            vm.KodPocztowy = "60101";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.KodPocztowy));
        }

        [Fact]
        public void TC_76_Edycja_Imie_Puste_Blad()
        {
            var vm = Poprawny();
            vm.FirstName = "";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.FirstName));
        }

        [Fact]
        public void TC_77_Edycja_Nazwisko_Puste_Blad()
        {
            var vm = Poprawny();
            vm.LastName = "";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.LastName));
        }

        [Fact]
        public void TC_78_Edycja_Haslo_BezCyfry_Blad()
        {
            var vm = Poprawny();
            vm.Password = "TestHaslo!";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.Password));
        }

        [Fact]
        public void TC_79_Edycja_Haslo_BezZnakuSpecjalnego_Blad()
        {
            var vm = Poprawny();
            vm.Password = "Test1234";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.Password));
        }

        [Fact]
        public void TC_80_Edycja_Status_Pusty_Blad()
        {
            var vm = Poprawny();
            vm.Status = "";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.Status));
        }

       
        [Fact]
        public void TC_82_Edycja_Miejscowosc_Pusta_Blad()
        {
            var vm = Poprawny();
            vm.Miejscowosc = "";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.Miejscowosc));
        }


        [Fact]
        public void TC_84_Edycja_NrPosesji_Pusty_Blad()
        {
            var vm = Poprawny();
            vm.NrPosesji = "";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
            AssertHasErrorFor(wyniki, nameof(UserVm.NrPosesji));
        }

        [Fact]
        public void TC_85_Edycja_WieleBledowNaraz()
        {
            var vm = Poprawny();
            vm.Email = "zly";
            vm.Pesel = "123";

            var (ok, wyniki) = Waliduj(vm);

            Assert.False(ok);
        }
    }
}