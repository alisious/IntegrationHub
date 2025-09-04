using IntegrationHub.SRP.Models;
using IntegrationHub.SRP.PESEL.SoapClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{
    public class FoundPerson
    {
        [JsonPropertyName("idOsoby")]
        public string? IdOsoby { get; set; }

        [JsonPropertyName("pesel")]
        public string? Pesel { get; set; }

        [JsonPropertyName("seriaINumerDowodu")]
        public string? SeriaINumerDowodu { get; set; }

        [JsonPropertyName("nazwisko")]
        public string? Nazwisko { get; set; }

        [JsonPropertyName("imiePierwsze")]
        public string? ImiePierwsze { get; set; }

        [JsonPropertyName("imieDrugie")]
        public string? ImieDrugie { get; set; }
        [JsonPropertyName("miejsceUrodzenia")]
        public string? MiejsceUrodzenia { get; set; }

        [JsonPropertyName("dataUrodzenia")]
        public string? DataUrodzenia { get; set; }

        [JsonPropertyName("plec")]
        public string? Plec { get; set; }
                
        [JsonPropertyName("czyZyje")]
        public bool? CzyZyje { get; set; }
        [JsonPropertyName("czyPeselAnulowany")]
        public bool? CzyPeselAnulowany { get; set; }

        [JsonPropertyName("zdjecie")]
        public string? Zdjecie { get; set; } = null!;
    }
}
