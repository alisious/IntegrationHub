using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.PIESP.Models
{
  
    /// <summary>
    /// Kod bezpieczeństwa przypisany do użytkownika, generowany przez przełożonego.
    /// </summary>
    public class SecurityCode
    {
        /// <summary>Identyfikator kodu.</summary>
        public int Id { get; set; }

        /// <summary>Numer odznaki użytkownika, dla którego wygenerowano kod.</summary>
        public required string BadgeNumber { get; set; }

        /// <summary>Właściwy 6-cyfrowy kod bezpieczeństwa.</summary>
        public required string Code { get; set; }

        /// <summary>Czas wygaśnięcia ważności kodu.</summary>
        public DateTime Expiry { get; set; }
    }

}
