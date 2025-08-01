using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Models
{
    public class BasicPersonDataResponse
    {
        [JsonPropertyName("znalezioneOsoby")]
        public List<BasicPersonData> Persons { get; set; } = new();
    }
}
