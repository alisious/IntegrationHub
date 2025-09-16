using IntegrationHub.SRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{
    public class GetCurrentIdByPeselResponse
    {
        [JsonPropertyName("dowod")]
        public DowodOsobisty? Dowod { get; set; }
    }
}
