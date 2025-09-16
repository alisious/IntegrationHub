using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Xml.Schema;
using System.Xml.Serialization;

namespace IntegrationHub.SRP.Models
{
    
    [XmlRoot("dowod")]
    public class DowodOsobisty
    {
        [XmlElement("dataWaznosci")]
        [JsonPropertyName("dataWaznosci")]
        public string? DataWaznosci { get; set; }
        [XmlElement("dataWydania")]
        [JsonPropertyName("dataWydania")]
        public string? DataWydania { get; set; }
        [XmlElement("seriaINumer")]
        [JsonPropertyName("seriaINumer")]
        public SeriaINumerDokumentuTozsamosci? SeriaINumer { get; set; }

        [XmlElement("daneOsobowe")]
        [JsonPropertyName("daneOsobowe")]

        public DaneOsobowe? DaneOsobowe { get; set; }

        public PodstawoweDaneUrodzenia? daneUrodzenia { get; set; }
        public DaneWystawcyDowodu? daneWystawcy { get; set; }

        [JsonPropertyName("zdjecieCzarnoBiale")]
        public string? ZdjecieCzarnoBiale { get; set; }
        
        [JsonPropertyName("zdjecieKolorowe")]
        public string? ZdjecieKolorowe { get; set; }

        public string? statusDokumentu { get; set; }
        public string? statusWarstwyEdo { get; set; }
        public string? obywatelstwo { get; set; }
        public string? idDowodu { get; set; }

        public string? kodTerytUrzeduWydajacego { get; set; } // dow:KodTerytUrzeduWydajacego – kod terytorialny urzędu wydającego
        public string? nazwaUrzeduWydajacego { get; set; } // dow:NazwaUrzeduWydajacego – nazwa urzędu wydającego

}


    public class DaneOsobowe
    {
        [XmlElement("imie")]
        public Imiona? imie { get; set; }

        [XmlElement("nazwisko")]
        public Nazwisko? nazwisko { get; set; }

        [JsonPropertyName("nazwiskoRodowe")]
        public string? nazwiskoRodowe { get; set; }
        
        [XmlElement("pesel")]
        public string? pesel { get; set; }
        [XmlElement("idOsoby")]
        public string? idOsoby { get; set; }

        
    }


    public class SeriaINumerDokumentuTozsamosci
    {
        [XmlElement("seriaDokumentuTozsamosci")]
        [JsonPropertyName("seriaDokumentuTozsamosci")]
        public string? seriaDokumentuTozsamosci { get; set; }

        [XmlElement("numerDokumentuTozsamosci")]
        [JsonPropertyName("numerDokumentuTozsamosci")]
        public string? numerDokumentuTozsamosci { get; set; }

        [JsonPropertyName("seriaNumerDowodu")]
        public string? SeriaNumerDowodu
        {
            get
            {
                if (string.IsNullOrWhiteSpace(seriaDokumentuTozsamosci) && string.IsNullOrWhiteSpace(numerDokumentuTozsamosci))
                    return null;
                return $"{seriaDokumentuTozsamosci}{numerDokumentuTozsamosci}";
            }
        }
    }

    public class Imiona
    {
        [XmlElement("imiePierwsze")]
        [JsonPropertyName("imiePierwsze")]
        public string? imiePierwsze { get; set; }

        [XmlElement("imieDrugie")]
        [JsonPropertyName("imieDrugie")]
        public string? imieDrugie { get; set; }
    }

    
    public class Nazwisko
    {
        [XmlElement("czlonPierwszy")]
        public string? czlonPierwszy { get; set; }

        [XmlElement("czlonDrugi")]
        public string? czlonDrugi { get; set; }
    }

    public class PodstawoweDaneUrodzenia
    {
        
        public string? dataUrodzenia { get; set; } 

        public string? imieMatki { get; set; }

        public string? imieOjca { get; set; }
        public string? miejsceUrodzenia { get; set; }
        public string? plec {  get; set; }
    }

    public class DaneWystawcyDowodu
    {
        
        public string? idOrganu { get; set; } // dow:IdOrgan – techniczne ID organu

       public string? nazwaWystawcy { get; set; } // nazwa organu wydającego z druku dowodu
    }

}
