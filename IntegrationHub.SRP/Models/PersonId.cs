using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Models
{
    
    public class PersonId
    {
        
        [JsonPropertyName("dataWaznosci")]
        public string? DataWaznosci { get; set; }

        [JsonPropertyName("dataWydania")]
        public string? DataWydania { get; set; }
        [JsonPropertyName("daneWystawcy")]
        public DaneWystawcy? DaneWystawcy { get; set; } = null;

        [JsonPropertyName("seriaINumer")]
        public SeriaINumer? SeriaINumer { get; set; }
        [JsonPropertyName("statusDokumentu")]
        public string? StatusDokumentu { get; set; }
        
        [JsonPropertyName("zdjecieCzarnoBiale")]
        public byte[] ZdjecieCzarnoBialeBase64 { get; set; } = Array.Empty<byte>();


    }



    public class SeriaINumer
    {
        [JsonPropertyName("seriaDokumentuTozsamosci")]
        public string? SeriaDokumentuTozsamosci { get; set; }

        [JsonPropertyName("numerDokumentuTozsamosci")]
        public string? NumerDokumentuTozsamosci { get; set; }
    }


    public class DaneWystawcy
    {
        [JsonPropertyName("idOrgan")]
        public string? IdOrgan { get; set; }

        [JsonPropertyName("nazwaWystawcyZDowodu")]
        public string? NazwaWystawcyZDowodu { get; set; }
    }

}
