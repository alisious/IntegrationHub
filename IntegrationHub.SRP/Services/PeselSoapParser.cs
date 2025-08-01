using IntegrationHub.SRP.PESEL.SoapClient;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;

namespace IntegrationHub.SRP.Services
{
    public class PeselSoapResult
    {
        public wyszukajPodstawoweDaneOsobyResponse? Response { get; set; }
        public BladTechniczny? BladTechniczny { get; set; }
        public BladBiznesowy? BladBiznesowy { get; set; }

        public bool IsSuccess => Response != null && (Response.znalezioneOsoby?.Any() ?? false);
        public bool IsNotFound => Response != null && (Response.znalezioneOsoby?.Length ?? 0) == 0;
        public bool IsFault => BladTechniczny != null || BladBiznesowy != null;
    }

    public interface IPeselSoapParser
    {
        PeselSoapResult ParseSoapResponse(string soapXml);
    }

    public class PeselSoapParser : IPeselSoapParser
    {
        private const string SoapNs = "http://schemas.xmlsoap.org/soap/envelope/";
        private const string PeselNs = "http://msw.gov.pl/srp/v3_0/uslugi/pesel/";

        private readonly ILogger<PeselSoapParser> _logger;

        public PeselSoapParser(ILogger<PeselSoapParser> logger)
        {
            _logger = logger;
        }

        public PeselSoapResult ParseSoapResponse(string soapXml)
        {
            var result = new PeselSoapResult();

            try
            {
                var ns = new XmlNamespaceManager(new NameTable());
                ns.AddNamespace("soap", SoapNs);
                ns.AddNamespace("ns2", PeselNs);

                var doc = new XmlDocument();
                doc.LoadXml(soapXml);

                result.BladTechniczny = TryDeserializeBladTechniczny(doc, ns);
                result.BladBiznesowy = TryDeserializeBladBiznesowy(doc, ns);
                result.Response = TryDeserializeResponse(doc, ns);
            }
            catch (XmlException xmlEx)
            {
                _logger.LogError(xmlEx, "Błąd parsowania XML: {Message}", xmlEx.Message);
                result.BladTechniczny = new BladTechniczny
                {
                    kod = "XML_PARSE_ERROR",
                    opis = "Niepoprawny format XML.",
                    opisTechniczny = xmlEx.Message
                };
            }
            catch (InvalidOperationException serEx)
            {
                _logger.LogError(serEx, "Błąd deserializacji SOAP: {Message}", serEx.Message);
                result.BladTechniczny = new BladTechniczny
                {
                    kod = "SOAP_DESERIALIZE_ERROR",
                    opis = "Nie można zdeserializować odpowiedzi SOAP.",
                    opisTechniczny = serEx.Message
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Nieoczekiwany błąd przy przetwarzaniu SOAP.");
                result.BladTechniczny = new BladTechniczny
                {
                    kod = "UNEXPECTED_ERROR",
                    opis = "Wystąpił nieoczekiwany błąd podczas przetwarzania odpowiedzi.",
                    opisTechniczny = ex.ToString()
                };
            }

            return result;
        }

        private static BladTechniczny? TryDeserializeBladTechniczny(XmlDocument doc, XmlNamespaceManager ns)
        {
            var node = doc.SelectSingleNode("//soap:Fault/detail/ns2:bladTechniczny", ns);
            if (node == null) return null;

            var serializer = new XmlSerializer(typeof(BladTechniczny), PeselNs);
            using var reader = new XmlNodeReader(node);
            return serializer.Deserialize(reader) as BladTechniczny;
        }

        private static BladBiznesowy? TryDeserializeBladBiznesowy(XmlDocument doc, XmlNamespaceManager ns)
        {
            var node = doc.SelectSingleNode("//soap:Fault/detail/ns2:bladBiznesowy", ns);
            if (node == null) return null;

            var serializer = new XmlSerializer(typeof(BladBiznesowy), PeselNs);
            using var reader = new XmlNodeReader(node);
            return serializer.Deserialize(reader) as BladBiznesowy;
        }

        private static wyszukajPodstawoweDaneOsobyResponse? TryDeserializeResponse(XmlDocument doc, XmlNamespaceManager ns)
        {
            var node = doc.SelectSingleNode("//ns2:wyszukajPodstawoweDaneOsobyResponse", ns);
            if (node == null) return null;

            var serializer = new XmlSerializer(typeof(wyszukajPodstawoweDaneOsobyResponse), PeselNs);
            using var reader = new XmlNodeReader(node);
            return serializer.Deserialize(reader) as wyszukajPodstawoweDaneOsobyResponse;
        }
    }
}

