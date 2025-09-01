using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{
    public class GetIdCardResponse
    {
        [JsonPropertyName("idCardXml")]
        public string? IdCardXml { get; set; } = string.Empty;
    }
}
