using System.Text.Json.Serialization;
namespace IntegrationHub.SRP.Models
{
    
    public class BasicPersonData
    {
        [JsonPropertyName("osobaId")]
        public string? OsobaId { get; set; }

        [JsonPropertyName("numerPesel")]
        public string? NumerPesel { get; set; }

        [JsonPropertyName("czyPeselAnulowany")]
        public bool? CzyPeselAnulowany { get; set; }

        [JsonPropertyName("imiePierwsze")]
        public string? ImiePierwsze { get; set; }

        [JsonPropertyName("imieDrugie")]
        public string? ImieDrugie { get; set; }

        [JsonPropertyName("imionaKolejne")]
        public string? ImionaKolejne { get; set; }

        [JsonPropertyName("nazwisko")]
        public string? Nazwisko { get; set; }

        [JsonPropertyName("nazwiskoRodowe")]
        public string? NazwiskoRodowe { get; set; }

        [JsonPropertyName("obywatelstwoKod")]
        public string? ObywatelstwoKod { get; set; }

        [JsonPropertyName("obywatelstwoNazwa")]
        public string? ObywatelstwoNazwa { get; set; }

        [JsonPropertyName("plec")]
        public string? Plec { get; set; }

        [JsonPropertyName("miejsceUrodzenia")]
        public string? MiejsceUrodzenia { get; set; }

        [JsonPropertyName("dataUrodzenia")]
        public string? DataUrodzenia { get; set; }

        [JsonPropertyName("imieMatki")]
        public string? ImieMatki { get; set; }

        [JsonPropertyName("imieOjca")]
        public string? ImieOjca { get; set; }

        [JsonPropertyName("nazwiskoRodoweMatki")]
        public string? NazwiskoRodoweMatki { get; set; }

        [JsonPropertyName("nazwiskoRodoweOjca")]
        public string? NazwiskoRodoweOjca { get; set; }

        [JsonPropertyName("danePobytuStalego")]
        public AddressData? DanePobytuStalego { get; set; }

        [JsonPropertyName("danePobytuCzasowego")]
        public AddressData? DanePobytuCzasowego { get; set; }

        [JsonPropertyName("czyZyje")]
        public bool? CzyZyje { get; set; }
    }
}


