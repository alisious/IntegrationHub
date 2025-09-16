using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Models;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntegrationHub.SRP.Extensions
{
    public static class RdoGetCurrentPhotoResponseXmlMapper
    {
        /// <summary>
        /// Parsuje SOAP XML "udostepnijAktualneZdjecie" do Base64 (bez dekodowania).
        /// Dla niepoprawnych elementow loguje WARNING + szczegoly (bez wylewania calej zawartosci).
        /// </summary>
        /// <param name="soapXml">Pelny SOAP XML.</param>
        /// <param name="logger">ILogger (Serilog jako provider).</param>
        /// <param name="requestId">Identyfikator korelacyjny z Twojego requestu.</param>
        /// <param name="validateBase64">Gdy true: sprawdzamy Convert.FromBase64String dla logu.</param>
        /// <param name="snippetChars">Ile znakow pokazac z poczatku/konca Base64 w logu.</param>
        public static GetCurrentPhotoResponse Parse(
            string soapXml,
            ILogger logger,
            string? requestId = null,
            bool validateBase64 = true,
            int snippetChars = 16)
        {
            if (string.IsNullOrWhiteSpace(soapXml))
                throw new ArgumentException("XML response is empty.", nameof(soapXml));

            // Zalecane: Serilog z Enrich.FromLogContext, aby wychwycic BeginScope
            using var _ = logger.BeginScope(new System.Collections.Generic.Dictionary<string, object?>
            {
                ["RequestId"] = requestId,
                ["Operation"] = "udostepnijAktualneZdjecie"
            });

            var doc = XDocument.Parse(soapXml, LoadOptions.PreserveWhitespace);
            XNamespace dow = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/";

            var responseNode = doc
                .Descendants()
                .FirstOrDefault(e => e.Name.LocalName == "udostepnijAktualneZdjecieResponse");

            var photoNodes = responseNode?
                .Descendants()
                .Where(e => e.Name.LocalName == "zdjecie")
                .ToList()
                ?? new List<XElement>();

            var result = new GetCurrentPhotoResponse();
            int invalid = 0, valid = 0;

            for (int i = 0; i < photoNodes.Count; i++)
            {
                var node = photoNodes[i];
                var base64 = (node.Value ?? string.Empty).Trim();
                if (base64.Length == 0)
                    continue;

                if (validateBase64)
                {
                    try
                    {
                        // Sama walidacja; nie trzymamy zdekodowanych bajtow.
                        Convert.FromBase64String(base64);
                    }
                    catch (FormatException ex)
                    {
                        invalid++;

                        // Krótkie, bezpieczne fragmenty (poczatek/koniec) zamiast pelnego payloadu
                        var first = base64.Length > snippetChars ? base64.Substring(0, snippetChars) : base64;
                        var last = base64.Length > snippetChars ? base64.Substring(Math.Max(0, base64.Length - snippetChars)) : string.Empty;
                        var padding = base64.Count(c => c == '=');

                        logger.LogWarning(
                            SrpLogEvents.InvalidBase64Photo,
                            ex,
                            "Invalid Base64 in photo item at index {PhotoIndex}. Length={Length}, Padding={Padding}, First{N}='{FirstSnippet}', Last{N}='{LastSnippet}'",
                            i, base64.Length, padding, snippetChars, first, snippetChars, last
                        );

                        // Pomijamy uszkodzony element
                        continue;
                    }
                }

                valid++;
                result.PhotosBase64.Add(base64);
            }

            logger.LogInformation(
                SrpLogEvents.ParseSummary,
                "Parsed current photos: OK={ValidCount}, SkippedInvalid={InvalidCount}, TotalNodes={Total}",
                valid, invalid, photoNodes.Count
            );

            return result;
        }


        



        

    }
}
