using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Models
{
    public class SearchPersonInputModel
    {
        [JsonPropertyName("requestId")]
        public required string RequestId { get; set; }

        [JsonPropertyName("badgeNumber")]
        public required string BadgeNumber { get; set; }

        [JsonPropertyName("unitName")]
        public required string UnitName { get; set; }

        [JsonPropertyName("pesel")]
        public string? Pesel { get; set; }

        [JsonPropertyName("imiePierwsze")]
        public string? ImiePierwsze { get; set; }
                
        [JsonPropertyName("nazwisko")]
        public string? Nazwisko { get; set; }

        [JsonPropertyName("dataUrodzenia")]
        public string? DataUrodzenia { get; set; }

        [JsonPropertyName("imieOjca")]
        public string? ImieOjca { get; set; }

        [JsonPropertyName("czyZyje")]
        public bool? CzyZyje { get; set; } = true;
    }

}
