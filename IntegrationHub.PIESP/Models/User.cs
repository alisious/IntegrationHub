using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.PIESP.Models
{

    // User.cs
    /// <summary>
    /// Reprezentuje użytkownika systemu PIESP.
    /// </summary>
    public class User
    {
        /// <summary>Identyfikator użytkownika.</summary>
        public int Id { get; set; }

        /// <summary>Stopień, imię i nazwisko użytkownika.</summary>
        public required string UserName { get; set; }

        /// <summary>Numer odznaki (unikalny identyfikator użytkownika).</summary>
        public required string BadgeNumber { get; set; }

        /// <summary>Hash kodu PIN przypisanego do użytkownika.</summary>
        public string? PinHash { get; set; }

        /// <summary>Lista ról przypisanych do użytkownika.</summary>
        public ICollection<UserRole> Roles { get; set; } = new List<UserRole>();
    }

}
