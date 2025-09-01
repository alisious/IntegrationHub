using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{
    public record GetCurrentPhotoRequest
    {

        [JsonPropertyName("pesel")]
        public string Pesel { get; set; } = string.Empty;
        [JsonPropertyName("idOsoby")]
        public string PersonId { get; set; } = string.Empty;
       
       
    }
}
