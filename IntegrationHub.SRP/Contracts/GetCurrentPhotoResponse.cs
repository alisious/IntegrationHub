using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Contracts
{
    public record GetCurrentPhotoResponse
    {
        [JsonPropertyName("listaZdjec")]
        public List<string> PhotosBase64 { get; init; } = new();

    }

    
}
