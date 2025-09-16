using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.Json.Serialization;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Models
{
    public class Osoba
    {
        [JsonPropertyName("czyAnulowano")]
        public bool? CzyAnulowano { get; set; }
        
        [JsonPropertyName("daneDowoduOsobistego")]
        public DaneDowoduOsobistego? DaneDowoduOsobistego { get; set; }

        [JsonPropertyName("daneImion")]
        public DaneImion? Imiona { get; set; }

        [JsonPropertyName("daneNazwiska")]
        public DaneNazwiska? Nazwiska { get; set; }

        [JsonPropertyName("daneObywatelstwa")]
        public string? Obywatelstwo { get; set; }

        [JsonPropertyName("daneUrodzenia")]
        public DaneUrodzenia? Urodzenie { get; set; }

        [JsonPropertyName("daneStanuCywilnego")]
        public DaneStanuCywilnego? StanCywilny { get; set; }

        [JsonPropertyName("danePaszportu")]
        public DanePaszportu? Paszport { get; set; }

        [JsonPropertyName("danePobytuStalego")]
        public DanePobytu? DanePobytuStalego { get; set; }

        [JsonPropertyName("danePobytuCzasowego")]
        public DanePobytu? DanePobytuCzasowego { get; set; }

        [JsonPropertyName("daneKrajowZamieszkania")]
        public DaneKrajowZamieszkania? KrajZamieszkania { get; set; }

        [JsonPropertyName("dataAktualizacji")]
        public string? DataAktualizacji { get; set; }

        [JsonPropertyName("idOsoby")]
        public string? IdOsoby { get; set; } = string.Empty;
        [JsonPropertyName("numerPesel")]
        public string? NumerPesel { get; set; }
    }

    public class DaneDowoduOsobistego
    {
        [JsonPropertyName("dataWaznosci")]
        public string? DataWaznosci { get; set; }

        [JsonPropertyName("seriaINumer")]
        public string? SeriaINumer { get; set; }

        [JsonPropertyName("wystawca")]
        public Organ? Wystawca { get; set; }

       
    }

    public class DaneImion
    {
        [JsonPropertyName("imiePierwsze")]
        public string? ImiePierwsze { get; set; }

        [JsonPropertyName("imieDrugie")]
        public string? ImieDrugie { get; set; }

      
    }

    public class DaneNazwiska
    {
        [JsonPropertyName("nazwisko")]
        public string? Nazwisko { get; set; }

        [JsonPropertyName("nazwiskoRodowe")]
        public string? NazwiskoRodowe { get; set; }

      
    }

    public class NazwiskoRodowe
    {
        [JsonPropertyName("nazwisko")]
        public string? Nazwisko { get; set; }
    }

    public class DaneObywatelstwa
    {
        [JsonPropertyName("obywatelstwo")]
        public string? Obywatelstwo { get; set; }

        [JsonPropertyName("kod")]
        public string? Kod { get; set; }

       
    }

    public class DanePaszportu
    {
        [JsonPropertyName("dataWaznosci")]
        public string? DataWaznosci { get; set; }

        [JsonPropertyName("seriaINumer")]
        public string? SeriaINumer { get; set; }

       
    }

    public class DanePobytu
    {
        [JsonPropertyName("adresZameldowaniaId")]
        public string? AdresZameldowaniaId { get; set; }

        [JsonPropertyName("numerDomu")]
        public string? NumerDomu { get; set; }
        [JsonPropertyName("numerLokalu")]
        public string? NumerLokalu { get; set; }

        [JsonPropertyName("gmina")]
        public string? Gmina { get; set; }
        [JsonPropertyName("kodPocztowy")]
        public string? KodPocztowy { get; set; }

        [JsonPropertyName("miejscowosc")]
        public string? Miejscowosc { get; set; }

        [JsonPropertyName("ulicaCecha")]
        public string? UlicaCecha { get; set; }
        [JsonPropertyName("ulicaNazwa")]
        public string? UlicaNazwa { get; set; }

        [JsonPropertyName("wojewodztwo")]
        public string? Wojewodztwo { get; set; }

        [JsonPropertyName("dataOd")]
        public string? DataOd { get; set; }
               
    }

    public class DaneKrajowZamieszkania
    {
        [JsonPropertyName("krajZamieszkania")]
        public string? KrajZamieszkania { get; set; }

        [JsonPropertyName("kod")]
        public string? Kod { get; set; }

    }

    public class DaneStanuCywilnego
    {
        [JsonPropertyName("dataZawarcia")]
        public string? DataZawarcia { get; set; }

        [JsonPropertyName("stanCywilny")]
        public string? StanCywilny { get; set; }

        [JsonPropertyName("numerAktu")]
        public string? NumerAktu { get; set; }
               
        [JsonPropertyName("wspolmalzonek")]
        public Wspolmalzonek? Wspolmalzonek { get; set; }
        [JsonPropertyName("czyZmienianoPlec")]
        public bool? CzyZmienianoPlec { get; set; }
    }

    public class Wspolmalzonek
    {
        [JsonPropertyName("imie")]
        public string? Imie { get; set; }

        [JsonPropertyName("nazwiskoRodowe")]
        public string? NazwiskoRodowe { get; set; }

        [JsonPropertyName("numerPesel")]
        public string? NumerPesel { get; set; }
    }

    public class DaneUrodzenia
    {
        [JsonPropertyName("dataUrodzenia")]
        public string? DataUrodzenia { get; set; }

        [JsonPropertyName("imieMatki")]
        public string? ImieMatki { get; set; }

        [JsonPropertyName("imieOjca")]
        public string? ImieOjca { get; set; }

        [JsonPropertyName("krajUrodzenia")]
        public string? KrajUrodzenia { get; set; }

        [JsonPropertyName("miejsceUrodzenia")]
        public string? MiejsceUrodzenia { get; set; }

        [JsonPropertyName("nazwiskoRodoweMatki")]
        public string? NazwiskoRodoweMatki { get; set; }

        [JsonPropertyName("nazwiskoRodoweOjca")]
        public string? NazwiskoRodoweOjca { get; set; }
        [JsonPropertyName("numerAktu")]
        public string? NumerAktu { get; set; }

        [JsonPropertyName("plec")]
        public string? Plec { get; set; }

       
    }

    public class Organ
    {
        [JsonPropertyName("kodTerytorialny")]
        public string? KodTerytorialny { get; set; }

        [JsonPropertyName("rodzajOrganu")]
        public string? RodzajOrganu { get; set; }
    }
}
