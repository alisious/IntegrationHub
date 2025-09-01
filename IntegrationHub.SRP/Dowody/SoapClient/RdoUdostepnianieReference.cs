// WCF client + modele dla udostepnijDaneAktualnychDowodowPoPesel
// SOAP 1.1, document/literal, XmlSerializer
using System;
using System.Collections.Generic;
using System.ServiceModel;
using System.ServiceModel.Channels;
using System.Threading.Tasks;
using System.Xml.Serialization;
using System.Xml.Schema;

namespace IntegrationHub.SRP.Dowody.SoapClient
{
    // ===== Service Contract (portType: DowodyUdostepnianie) =====
    [ServiceContract(Name = "DowodyUdostepnianie", Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/")]
    [XmlSerializerFormat]
    public interface IDowodyUdostepnianie
    {
        [OperationContract(
            Action = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/Udostepnianie/udostepnijDaneAktualnychDowodowPoPesel/",
            ReplyAction = "*")]
        [FaultContract(typeof(BladBiznesowy), Name = "bladBiznesowy", Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/")]
        [FaultContract(typeof(BladTechniczny), Name = "bladTechniczny", Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/")]
        Task<udostepnijDaneAktualnychDowodowPoPeselResponse> udostepnijDaneAktualnychDowodowPoPeselAsync(
            udostepnijDaneAktualnychDowodowPoPesel request);
    }

    // ===== Client =====
    public sealed class DowodyUdostepnianieClient : ClientBase<IDowodyUdostepnianie>, IDowodyUdostepnianie
    {
        public DowodyUdostepnianieClient(Binding binding, EndpointAddress remoteAddress)
            : base(binding, remoteAddress) { }

        public DowodyUdostepnianieClient(string endpointUrl, bool requireClientCertificate = false)
            : base(CreateDefaultBinding(endpointUrl, requireClientCertificate), new EndpointAddress(endpointUrl)) { }

        private static Binding CreateDefaultBinding(string url, bool requireClientCertificate)
        {
            var https = url?.StartsWith("https", StringComparison.OrdinalIgnoreCase) == true;
            var security = https ? BasicHttpSecurityMode.Transport : BasicHttpSecurityMode.None;

            var b = new BasicHttpBinding(security)
            {
                MessageEncoding = WSMessageEncoding.Text,
                MaxBufferSize = 32 * 1024 * 1024,
                MaxReceivedMessageSize = 64L * 1024L * 1024L,
                ReaderQuotas =
                {
                    MaxDepth = 64,
                    MaxArrayLength = 16 * 1024 * 1024,
                    MaxStringContentLength = 16 * 1024 * 1024
                }
            };
            if (https)
            {
                b.Security.Transport.ClientCredentialType =
                    requireClientCertificate ? HttpClientCredentialType.Certificate : HttpClientCredentialType.None;
            }
            return b;
        }

        public Task<udostepnijDaneAktualnychDowodowPoPeselResponse> udostepnijDaneAktualnychDowodowPoPeselAsync(
            udostepnijDaneAktualnychDowodowPoPesel request)
            => Channel.udostepnijDaneAktualnychDowodowPoPeselAsync(request);
    }

    // ===== Request wrapper (element w ns: uslugi/dowody) =====
    [XmlRoot("udostepnijDaneAktualnychDowodowPoPesel", Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/")]
    public class udostepnijDaneAktualnychDowodowPoPesel
    {
        [XmlElement("listaNumerowPesel", Form = XmlSchemaForm.Qualified)]
        public ListaNumerowPesel listaNumerowPesel { get; set; } = new ListaNumerowPesel();
    }

    // ===== Response wrapper (element w ns: uslugi/dowody) =====
    [XmlRoot("udostepnijDaneAktualnychDowodowPoPeselResponse", Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/")]
    public class udostepnijDaneAktualnychDowodowPoPeselResponse
    {
        [XmlElement("dowody", Form = XmlSchemaForm.Qualified)]
        public ListaDowodowZUdostepnianymiDanymi dowody { get; set; } = new ListaDowodowZUdostepnianymiDanymi();
    }

    // ===== Parametry/udostepnianie =====
    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/parametry/udostepnianie/")]
    public class ListaNumerowPesel
    {
        [XmlElement("numerPesel", Form = XmlSchemaForm.Qualified)]
        public List<string> numerPesel { get; set; } = new List<string>();
    }

    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/parametry/udostepnianie/")]
    public class ListaDowodowZUdostepnianymiDanymi
    {
        [XmlElement("dowod", Form = XmlSchemaForm.Qualified)]
        public List<DaneDowodu> dowod { get; set; } = new List<DaneDowodu>();
    }

    // ===== Domena/dowody (podzbiór potrzebny do mapowania) =====
    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/domena/dowody/")]
    public class DaneDowodu
    {
        // PodstawoweDaneDokumentuTozsamosci
        [XmlElement("dataWaznosci", Form = XmlSchemaForm.Qualified)]
        public string? dataWaznosci { get; set; }         // RRRRMMDD (kmd:Data)

        [XmlElement("dataWydania", Form = XmlSchemaForm.Qualified)]
        public string? dataWydania { get; set; }          // RRRRMMDD (kmd:Data)

        [XmlElement("seriaINumer", Form = XmlSchemaForm.Qualified)]
        public SeriaINumerDokumentuTozsamosci? seriaINumer { get; set; }

        // Z rozszerzenia DaneFormularzaDowodu → dane wystawcy (organ)
        [XmlElement("daneWystawcy", Form = XmlSchemaForm.Qualified)]
        public DaneWystawcyDowod? daneWystawcy { get; set; }

        // Dane osoby
        [XmlElement("daneOsobowe", Form = XmlSchemaForm.Qualified)]
        public Osoba? daneOsobowe { get; set; }

        [XmlElement("podstawoweDaneUrodzeniaDowod", Form = XmlSchemaForm.Qualified)]
        public PodstawoweDaneUrodzeniaDowod? podstawoweDaneUrodzeniaDowod { get; set; }

        [XmlElement("zdjecieCzarnoBiale", Form = XmlSchemaForm.Qualified, DataType = "base64Binary")]
        public byte[]? zdjecieCzarnoBiale { get; set; }

        // (opcjonalnie – skoro w XSD też występuje)
        [XmlElement("zdjecieKolorowe", Form = XmlSchemaForm.Qualified, DataType = "base64Binary")]
        public byte[]? zdjecieKolorowe { get; set; }
        [XmlAnyElement] public System.Xml.XmlElement[]? Any { get; set; }
    }

    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/domena/dowody/")]
    public class SeriaINumerDokumentuTozsamosci
    {
        [XmlElement("seriaDokumentuTozsamosci", Form = XmlSchemaForm.Qualified)]
        public string? seriaDokumentuTozsamosci { get; set; }

        [XmlElement("numerDokumentuTozsamosci", Form = XmlSchemaForm.Qualified)]
        public string? numerDokumentuTozsamosci { get; set; }
    }

    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/domena/dowody/")]
    public class DaneWystawcyDowod
    {
        [XmlElement("idOrgan", Form = XmlSchemaForm.Qualified)]
        public string? idOrgan { get; set; } // dow:IdOrgan – techniczne ID organu

        [XmlElement("nazwaWystawcyZDowodu", Form = XmlSchemaForm.Qualified)]
        public string? nazwaWystawcyZDowodu { get; set; } // nazwa organu wydającego z druku dowodu
    }

    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/domena/dowody/")]
    public class Osoba
    {
        [XmlElement("imie", Form = XmlSchemaForm.Qualified)]
        public Imiona? imie { get; set; }

        [XmlElement("nazwisko", Form = XmlSchemaForm.Qualified)]
        public Nazwisko? nazwisko { get; set; }

        [XmlElement("pesel", Form = XmlSchemaForm.Qualified)]
        public string? pesel { get; set; }
    }

    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/domena/dowody/")]
    public class Imiona
    {
        [XmlElement("imiePierwsze", Form = XmlSchemaForm.Qualified)]
        public string? imiePierwsze { get; set; }

        [XmlElement("imieDrugie", Form = XmlSchemaForm.Qualified)]
        public string? imieDrugie { get; set; }
    }

    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/domena/dowody/")]
    public class Nazwisko
    {
        [XmlElement("czlonPierwszy", Form = XmlSchemaForm.Qualified)]
        public string? czlonPierwszy { get; set; }

        [XmlElement("czlonDrugi", Form = XmlSchemaForm.Qualified)]
        public string? czlonDrugi { get; set; }
    }

    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/domena/dowody/")]
    public class PodstawoweDaneUrodzeniaDowod
    {
        [XmlElement("dataUrodzenia", Form = XmlSchemaForm.Qualified)]
        public string? dataUrodzenia { get; set; } // RRRRMMDD

        [XmlElement("imieMatki", Form = XmlSchemaForm.Qualified)]
        public string? imieMatki { get; set; }

        [XmlElement("imieOjca", Form = XmlSchemaForm.Qualified)]
        public string? imieOjca { get; set; }
    }

    // ===== Faults (wspolne) =====
    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/wspolne/")]
    public class BladTechniczny
    {
        [XmlElement("kod", Form = XmlSchemaForm.Qualified)]
        public string? kod { get; set; }                      // wsp:KodBledu

        [XmlElement("opis", Form = XmlSchemaForm.Qualified)]
        public string? opis { get; set; }                     // string

        [XmlElement("opisTechniczny", Form = XmlSchemaForm.Qualified)]
        public string? opisTechniczny { get; set; }           // string
    }

    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/wspolne/")]
    public class BladBiznesowy
    {
        [XmlElement("kod", Form = XmlSchemaForm.Qualified)]
        public string? kod { get; set; }                      // wsp:KodBledu

        [XmlElement("opis", Form = XmlSchemaForm.Qualified)]
        public string? opis { get; set; }                     // kmd:KrotkiTekst → string

        [XmlElement("szczegoly", Form = XmlSchemaForm.Qualified)]
        public List<SzczegolyBledu>? szczegoly { get; set; }  // 0..n
    }

    [XmlType(Namespace = "http://msw.gov.pl/srp/v3_0/uslugi/wspolne/")]
    public class SzczegolyBledu
    {
        [XmlElement("kod", Form = XmlSchemaForm.Qualified)]
        public string? kod { get; set; }                      // wsp:KodBledu

        [XmlElement("zrodlo", Form = XmlSchemaForm.Qualified)]
        public string? zrodlo { get; set; }                   // kmd:KrotkiTekst → string

        [XmlElement("opis", Form = XmlSchemaForm.Qualified)]
        public string? opis { get; set; }                     // kmd:KrotkiTekst → string
    }
}
