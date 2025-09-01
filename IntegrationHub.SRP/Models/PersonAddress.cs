using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Models
{
    public class PersonAddress
    {
        [JsonPropertyName("numerDomu")]
        public string? NumerDomu { get; set; }

        [JsonPropertyName("gminaNazwa")]
        public string? GminaNazwa { get; set; }

        [JsonPropertyName("gminaKod")]
        public string? GminaKodTeryt { get; set; }

        [JsonPropertyName("kodPocztowy")]
        public string? KodPocztowy { get; set; }

        [JsonPropertyName("numerLokalu")]
        public string? NumerLokalu { get; set; }

        [JsonPropertyName("miejscowoscNazwa")]
        public string? MiejscowoscNazwa { get; set; }

        [JsonPropertyName("miejscowoscKodTeryt")]
        public string? MiejscowoscKod { get; set; }

        [JsonPropertyName("ulicaCecha")]
        public string? UlicaCecha { get; set; }

        [JsonPropertyName("ulicaNazwa")]
        public string? UlicaNazwa { get; set; }

        [JsonPropertyName("ulicaKodTeryt")]
        public string? UlicaKodTeryt { get; set; }

        [JsonPropertyName("wojewodztwoNazwa")]
        public string? WojewodztwoNazwa { get; set; }

        [JsonPropertyName("wojewodztwoKodTeryt")]
        public string? WojewodztwoKodTeryt { get; set; }

        [JsonPropertyName("komentarz")]
        public string? Komentarz { get; set; }

        [JsonPropertyName("dataOd")]
        public string? DataOd { get; set; }

        [JsonPropertyName("dataDo")]
        public string? DataDo { get; set; }
    }
}
