using IntegrationHub.SRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{
    public class SearchPersonResponse
    {

        [JsonPropertyName("znalezioneOsoby")]
        public List<PersonData> Persons { get; set; } = new();
        
    }
}
