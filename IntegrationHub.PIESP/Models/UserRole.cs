using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.PIESP.Models
{
    /// <summary>
    /// Typy ról przypisywanych użytkownikowi.
    /// </summary>
    public enum RoleType { User, Supervisor, PowerUser }

    /// <summary>
    /// Powiązanie użytkownika z konkretną rolą w systemie.
    /// </summary>
    public class UserRole
    {
        /// <summary>Identyfikator powiązania roli z użytkownikiem.</summary>
        public int Id { get; set; }

        /// <summary>Rodzaj przypisanej roli.</summary>
        public RoleType Role { get; set; }

        /// <summary>Identyfikator użytkownika (klucz obcy).</summary>
        public int UserId { get; set; }

        /// <summary>Referencja do użytkownika.</summary>
        public User User { get; set; }
    }

}
