using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Models;

namespace IntegrationHub.SRP.Extensions
{ 
    /// <summary>
    /// Mapuje SOAP XML z wyszukajOsobyResponse -> SearchPersonResponse (List\PersonData\).
    /// Działa na strukturze jak w przykładzie PESEL_wyszukajOsoby_13_Response.xml.
    /// </summary>
    public static class PeselSearchPersonResponseXmlMapper
    {
        private static readonly XNamespace Ns = "http://msw.gov.pl/srp/v3_0/uslugi/pesel/";

        /// <summary>
        /// Główna metoda – podaj surowy SOAP XML.
        /// </summary>
        public static SearchPersonResponse Parse(string soapXml)
        {
            if (string.IsNullOrWhiteSpace(soapXml))
                throw new ArgumentException("soapXml is null or empty", nameof(soapXml));

            var doc = XDocument.Parse(soapXml, LoadOptions.PreserveWhitespace);

            // Wrapper jest w ns: http://msw.gov.pl/srp/v3_0/uslugi/pesel/
            var wrapper = doc.Descendants(Ns + "wyszukajOsobyResponse").FirstOrDefault();
            if (wrapper == null)
                throw new InvalidOperationException("Nie znaleziono elementu wyszukajOsobyResponse w podanym XML.");

            // Wewnętrzne elementy (znalezioneOsoby / znalezionaOsoba / pola) są UNQUALIFIED (bez namespace'u)
            var listEl = wrapper.Element("znalezioneOsoby");
            if (listEl == null)
                return new SearchPersonResponse();

            var persons = listEl.Elements("znalezionaOsoba")
                                .Select(MapOsoba)
                                .ToList();

            return new SearchPersonResponse { Persons = persons };
        }

        private static OsobaZnaleziona MapOsoba(XElement el)
        {
            var osoba = new OsobaZnaleziona
            {
                IdOsoby = V(el, "idOsoby"),
                Pesel = V(el, "pesel"),
                SeriaINumerDowodu = V(el, "seriaINumerDowodu"),
                ImiePierwsze = V(el, "imiePierwsze"),
                ImieDrugie = V(el, "imieDrugie"),
                Nazwisko = V(el, "nazwisko"),
                MiejsceUrodzenia = V(el, "miejsceUrodzenia"),
                DataUrodzenia = V(el, "dataUrodzenia"),
                Plec = V(el, "plec"),              // np. "MEZCZYZNA" / "KOBIETA"
                CzyZyje = VB(el, "czyZyje"),
                // W odpowiedzi jest "czyAnulowany" – mapujemy to na CzyPeselAnulowany w DTO
                CzyPeselAnulowany = VB(el, "czyAnulowany"),
                Zdjecie = null // Zdjęcie nie jest zwracane w XML, więc ustawiamy na null

            };
             

            return osoba;
        }

        private static string? V(XElement el, string name)
            => el.Element(name)?.Value?.Trim();

        private static bool? VB(XElement el, string name)
        {
            var raw = V(el, name);
            return bool.TryParse(raw, out var b) ? b : null;
        }
    }
}
