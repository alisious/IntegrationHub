using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml.Linq;
using IntegrationHub.SRP.Contracts;

namespace IntegrationHub.SRP.Helpers
{
    /// <summary>
    /// Mapuje SOAP XML z udostepnijAktualneZdjecieResponse -> GetCurrentPhotoResponse.
    /// Działa na strukturze jak w przykładzie RDO_udostepnijAktualneZdjecie_Response.xml.
    /// </summary>
    public static class RdoGetCurrentPhotoResponseXmlMapper
    {
        private static readonly XNamespace Ns = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/";

        /// <summary>
        /// Główna metoda – podaj surowy SOAP XML.
        /// Zwraca listę zdjęć (Base64) w GetCurrentPhotoResponse.PhotosBase64.
        /// </summary>
        public static GetCurrentPhotoResponse Parse(string soapXml)
        {
            if (string.IsNullOrWhiteSpace(soapXml))
                throw new ArgumentException("soapXml is null or empty", nameof(soapXml));

            var doc = XDocument.Parse(soapXml, LoadOptions.PreserveWhitespace);

            // Wrapper jest w ns: http://msw.gov.pl/srp/v3_0/uslugi/dowody/
            var wrapper = doc.Descendants(Ns + "udostepnijAktualneZdjecieResponse").FirstOrDefault();
            if (wrapper == null)
                throw new InvalidOperationException("Nie znaleziono elementu udostepnijAktualneZdjecieResponse w podanym XML.");

            // Wewnętrzne elementy są UNQUALIFIED (bez namespace'u)
            var photos = wrapper.Element("listaZdjec")?
                                .Elements("zdjecie")
                                .Select(x => (x.Value ?? string.Empty).Trim())
                                .Where(v => !string.IsNullOrWhiteSpace(v))
                                .ToList()
                         ?? new List<string>();

            return new GetCurrentPhotoResponse { PhotosBase64 = photos };
        }

        /// <summary>
        /// (Opcjonalnie) Szybki helper: zwróć pierwsze zdjęcie jako bajty (lub null, gdy brak/niepoprawne Base64).
        /// </summary>
        public static byte[]? ParseFirstPhotoBytes(string soapXml)
        {
            var b64 = Parse(soapXml).PhotosBase64.FirstOrDefault();
            if (string.IsNullOrWhiteSpace(b64)) return null;

            try { return Convert.FromBase64String(b64); }
            catch { return null; }
        }

        /// <summary>
        /// Zwraca pierwsze zdjęcie jako base64 (string) lub null – bezpośrednio z XML.
        /// </summary>
        public static string? ParseFirstPhotoBase64(string soapXml)
        {
            if (string.IsNullOrWhiteSpace(soapXml)) return null;

            var doc = XDocument.Parse(soapXml, LoadOptions.PreserveWhitespace);
            var wrapper = doc.Descendants(Ns + "udostepnijAktualneZdjecieResponse").FirstOrDefault();

            var first = wrapper?
                .Element("listaZdjec")?
                .Elements("zdjecie")
                .Select(x => (x.Value ?? string.Empty).Trim())
                .FirstOrDefault(v => !string.IsNullOrWhiteSpace(v));

            return string.IsNullOrWhiteSpace(first) ? null : first;
        }

        /// <summary>
        /// Wersja na już zmapowany obiekt – zwraca pierwsze zdjęcie (base64) lub null.
        /// </summary>
        public static string? FirstPhotoOrNull(GetCurrentPhotoResponse resp)
            => resp?.PhotosBase64?.FirstOrDefault(s => !string.IsNullOrWhiteSpace(s));
    }
}

