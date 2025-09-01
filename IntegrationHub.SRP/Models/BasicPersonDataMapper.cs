using IntegrationHub.SRP.PESEL.SoapClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Models
{
    public static class BasicPersonDataMapper
    {
        public static BasicPersonDataResponse MapToJsonDto(wyszukajPodstawoweDaneOsobyResponse response)
        {
            var result = new BasicPersonDataResponse();

            if (response?.znalezioneOsoby != null)
            {
                foreach (var os in response.znalezioneOsoby)
                {
                    result.Persons.Add(new PersonData
                    {
                        OsobaId = os.osobaId,
                        NumerPesel = os.numerPesel,
                        CzyPeselAnulowany = os.czyPeselAnulowany,
                        ImiePierwsze = os.imiePierwsze,
                        ImieDrugie = os.imieDrugie,
                        ImionaKolejne = os.imionaKolejne,
                        Nazwisko = os.nazwisko,
                        NazwiskoRodowe = os.nazwiskoRodowe,
                        ObywatelstwoKod = os.obywatelstwo?.kod,
                        ObywatelstwoNazwa = os.obywatelstwo?.Value,
                        Plec = os.plec.ToString(),
                        MiejsceUrodzenia = os.miejsceUrodzenia,
                        DataUrodzenia = os.dataUrodzenia,
                        ImieMatki = os.imieMatki,
                        ImieOjca = os.imieOjca,
                        NazwiskoRodoweMatki = os.nazwiskoRodoweMatki,
                        NazwiskoRodoweOjca = os.nazwiskoRodoweOjca,
                        DanePobytuStalego = os.danePobytuStalego == null ? null : new PersonAddress
                        {
                            NumerDomu = os.danePobytuStalego.numerDomu,
                            GminaNazwa = os.danePobytuStalego.gmina?.Value,
                            GminaKodTeryt = os.danePobytuStalego.gmina?.kt,
                            KodPocztowy = os.danePobytuStalego.kodPocztowy,
                            NumerLokalu = os.danePobytuStalego.numerLokalu,
                            MiejscowoscNazwa = os.danePobytuStalego.miejscowoscDzielnica?.nazwaMiejscowosci,
                            MiejscowoscKod = os.danePobytuStalego.miejscowoscDzielnica?.kodTerytorialny,
                            UlicaCecha = os.danePobytuStalego.ulica?.cecha,
                            UlicaNazwa = os.danePobytuStalego.ulica?.nazwaPierwsza,
                            WojewodztwoNazwa = os.danePobytuStalego.wojewodztwo?.Value,
                            WojewodztwoKodTeryt = os.danePobytuStalego.wojewodztwo?.woj,
                            Komentarz = os.danePobytuStalego.komentarz,
                            DataOd = os.danePobytuStalego.dataOd,
                            DataDo = os.danePobytuStalego.dataDo
                        },
                        DanePobytuCzasowego = os.danePobytuCzasowego == null ? null : new PersonAddress
                        {
                            // Analogicznie jak wyżej
                            NumerDomu = os.danePobytuCzasowego.numerDomu,
                            GminaNazwa = os.danePobytuCzasowego.gmina?.Value,
                            GminaKodTeryt = os.danePobytuCzasowego.gmina?.kt,
                            KodPocztowy = os.danePobytuCzasowego.kodPocztowy,
                            NumerLokalu = os.danePobytuCzasowego.numerLokalu,
                            MiejscowoscNazwa = os.danePobytuCzasowego.miejscowoscDzielnica?.nazwaMiejscowosci,
                            MiejscowoscKod = os.danePobytuCzasowego.miejscowoscDzielnica?.kodTerytorialny,
                            UlicaCecha = os.danePobytuCzasowego.ulica?.cecha,
                            UlicaNazwa = os.danePobytuCzasowego.ulica?.nazwaPierwsza,
                            WojewodztwoNazwa = os.danePobytuCzasowego.wojewodztwo?.Value,
                            WojewodztwoKodTeryt = os.danePobytuCzasowego.wojewodztwo?.woj,
                            Komentarz = os.danePobytuCzasowego.komentarz,
                            DataOd = os.danePobytuCzasowego.dataOd,
                            DataDo = os.danePobytuCzasowego.dataDo
                        },
                        CzyZyje = os.czyZyje
                    });
                }
            }
            return result;
        }
    }
}
