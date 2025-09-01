using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{

    /// <summary>
    /// Zapytanie pobierania aktualnych danych osoby z SRP (PESEL).
    /// Wymagane: podaj <b>OsobaId</b> tj. systemowy identyfikator osoby w SRP.
    /// </summary>
    public record GetPersonRequest
    {
        [JsonPropertyName("osobaId")]
        public string? OsobaId { get; set; }
       
    }



}
