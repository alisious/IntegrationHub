using System;
using System.IO;
using System.Linq;
using System.Text;
using System.Xml;
using System.Xml.Linq;
using System.Xml.Serialization;
using IntegrationHub.SRP.PESEL.SoapClient;   // wyszukajPodstawoweDaneOsobyResponse + modele z svcutil
using IntegrationHub.SRP.Models;             // BasicPersonDataResponse, PersonData, PersonAddress (opcjonalnie)

namespace IntegrationHub.SRP.Helpers
{
    public static class PeselSoapXmlHelper
    {
        private static readonly XNamespace Soap11 = "http://schemas.xmlsoap.org/soap/envelope/";
        private static readonly XNamespace Soap12 = "http://www.w3.org/2003/05/soap-envelope";
        private static readonly XNamespace PeselNs = "http://msw.gov.pl/srp/v3_0/uslugi/pesel/";

        /// <summary>
        /// Parsuje cały SOAP XML i zwraca obiekt wygenerowanej klasy MessageContract:
        /// wyszukajPodstawoweDaneOsobyResponse (z WyszukiwanieReference.cs).
        /// Rzuca wyjątek z czytelnym komunikatem, jeśli czegoś brakuje.
        /// </summary>
        public static wyszukajPodstawoweDaneOsobyResponse ParseWyszukajPodstawoweDaneOsobyResponse(string soapXml)
        {
            if (string.IsNullOrWhiteSpace(soapXml))
                throw new ArgumentException("soapXml is null or empty.", nameof(soapXml));

            var doc = XDocument.Parse(soapXml, LoadOptions.PreserveWhitespace);

            // Obsługa SOAP 1.1 i 1.2
            var body = doc.Root?.Element(Soap11 + "Body") ?? doc.Root?.Element(Soap12 + "Body");
            if (body == null)
                throw new InvalidOperationException("Nie znaleziono elementu SOAP Body.");

            var respEl = body.Element(PeselNs + "wyszukajPodstawoweDaneOsobyResponse");
            if (respEl == null)
                throw new InvalidOperationException(
                    "Nie znaleziono elementu 'wyszukajPodstawoweDaneOsobyResponse' w oczekiwanej przestrzeni nazw 'http://msw.gov.pl/srp/v3_0/uslugi/pesel/'.");

            // Ustal jawnie korzeń do XmlSerializer, bo MessageContract nie ma [XmlRoot]
            var root = new XmlRootAttribute("wyszukajPodstawoweDaneOsobyResponse")
            {
                Namespace = PeselNs.NamespaceName,
                IsNullable = false
            };

            var serializer = new XmlSerializer(typeof(wyszukajPodstawoweDaneOsobyResponse), root);

            using var reader = respEl.CreateReader();
            if (serializer.Deserialize(reader) is wyszukajPodstawoweDaneOsobyResponse result)
                return result;

            throw new InvalidOperationException("Deserializacja nie zwróciła obiektu odpowiedzi.");
        }

        /// <summary>Wersja Stream → Response.</summary>
        public static wyszukajPodstawoweDaneOsobyResponse ParseWyszukajPodstawoweDaneOsobyResponse(Stream soapStream)
        {
            using var sr = new StreamReader(soapStream, Encoding.UTF8, detectEncodingFromByteOrderMarks: true, bufferSize: 1024, leaveOpen: true);
            var xml = sr.ReadToEnd();
            return ParseWyszukajPodstawoweDaneOsobyResponse(xml);
        }

        /// <summary>Wersja TryParse (bez wyjątków).</summary>
        public static bool TryParseWyszukajPodstawoweDaneOsobyResponse(
            string soapXml,
            out wyszukajPodstawoweDaneOsobyResponse? response,
            out string? error)
        {
            try
            {
                response = ParseWyszukajPodstawoweDaneOsobyResponse(soapXml);
                error = null;
                return true;
            }
            catch (Exception ex)
            {
                response = null;
                error = ex.Message;
                return false;
            }
        }

        // =====================  OPCJONALNIE: mapowanie do Twoich DTO  =====================

        public static BasicPersonDataResponse ToBasicDto(wyszukajPodstawoweDaneOsobyResponse response)
        {
            var dto = new BasicPersonDataResponse();

            var items = response?.znalezioneOsoby ?? Array.Empty<PodstawoweDaneOsobyZDanymiPobytu>();
            dto.Persons = items.Select(MapToPersonData).ToList();

            return dto;
        }

        private static PersonData MapToPersonData(PodstawoweDaneOsobyZDanymiPobytu s)
        {
            return new PersonData
            {
                OsobaId = s.osobaId,
                NumerPesel = s.numerPesel,
                CzyPeselAnulowany = s.czyPeselAnulowany,
                ImiePierwsze = s.imiePierwsze,
                ImieDrugie = s.imieDrugie,
                ImionaKolejne = s.imionaKolejne,
                Nazwisko = s.nazwisko,
                NazwiskoRodowe = s.nazwiskoRodowe,
                ObywatelstwoKod = s.obywatelstwo?.kod,
                ObywatelstwoNazwa = s.obywatelstwo?.Value,
                Plec = s.plec.ToString(), // MEZCZYZNA/KOBIETA
                MiejsceUrodzenia = s.miejsceUrodzenia,
                DataUrodzenia = s.dataUrodzenia,
                ImieMatki = s.imieMatki,
                ImieOjca = s.imieOjca,
                NazwiskoRodoweMatki = s.nazwiskoRodoweMatki,
                NazwiskoRodoweOjca = s.nazwiskoRodoweOjca,
                DanePobytuStalego = MapAddress(s.danePobytuStalego),
                DanePobytuCzasowego = MapAddress(s.danePobytuCzasowego),
                CzyZyje = s.czyZyje,
            };
        }

        private static PersonAddress? MapAddress(PodstawoweDanePobytuOut a)
        {
            if (a == null) return null;

            return new PersonAddress
            {
                NumerDomu = a.numerDomu,
                GminaNazwa = a.gmina?.Value,
                GminaKodTeryt = a.gmina?.kt,
                KodPocztowy = a.kodPocztowy,
                NumerLokalu = a.numerLokalu,
                MiejscowoscNazwa = a.miejscowoscDzielnica?.nazwaMiejscowosci,
                MiejscowoscKod = a.miejscowoscDzielnica?.kodTerytorialny,
                UlicaCecha = a.ulica?.cecha,
                UlicaNazwa = a.ulica?.nazwaPierwsza,
                UlicaKodTeryt = a.ulica?.symbolUlicy,
                WojewodztwoNazwa = a.wojewodztwo?.Value,
                WojewodztwoKodTeryt = a.wojewodztwo?.woj,
                Komentarz = a.komentarz,
                DataOd = a.dataOd,
                DataDo = a.dataDo
            };
        }
    }
}
