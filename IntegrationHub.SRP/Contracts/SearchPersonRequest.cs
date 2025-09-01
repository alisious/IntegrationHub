using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{

    /// <summary>
    /// Zapytanie wyszukiwania osoby w SRP (PESEL).
    /// Wymagane: podaj <b>PESEL</b> albo zestaw: <b>Nazwisko</b> i <b>Imię</b>.
    /// Pole <c>dataUrodzenia</c> oczekuje formatu <c>yyyyMMdd</c> lub <c>yyyy-MM-dd</c>.
    /// </summary>
    public record SearchPersonRequest
    {
        [JsonPropertyName("pesel")]
        public string? Pesel { get; set; }
        [JsonPropertyName("nazwisko")]
        public string? Nazwisko { get; set; }
        [JsonPropertyName("imie")]
        public string? ImiePierwsze { get; set; }
        [JsonPropertyName("imieDrugie")]
        public string? ImieDrugie { get; set; }
        [JsonPropertyName("dataUrodzenia")]
        public string? DataUrodzenia { get; set; } // przyjmie np. "1973-10-29" lub "19731029"
        [JsonPropertyName("imieOjca")]
        public string? ImieOjca { get; set; }
        [JsonPropertyName("imieMatki")]
        public string? ImieMatki { get; set; }
    }



}
