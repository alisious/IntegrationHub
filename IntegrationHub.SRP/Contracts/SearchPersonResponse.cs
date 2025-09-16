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
        [JsonPropertyName("liczbaZnalezionychOsob")]
        public int TotalCount => Persons.Count;
        [JsonPropertyName("znalezioneOsoby")]
        public List<OsobaZnaleziona> Persons { get; set; } = new();
       

    }
}
