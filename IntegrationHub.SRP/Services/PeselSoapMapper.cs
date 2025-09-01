using IntegrationHub.Common.Helpers;
using IntegrationHub.SRP.Interfaces;
using IntegrationHub.SRP.Models;
using IntegrationHub.SRP.PESEL.SoapClient;
using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml;
using System.Xml.Serialization;
using System.Security;

namespace IntegrationHub.SRP.Services
{
    public class PeselSoapMapper : IPeselSoapMapper
    {
        

        public string BuildSearchByPeselRequestEnvelopeXml(SearchPersonInputModel input)
        {
            // Możesz rozszerzyć XML o inne elementy jeśli będą wymagane przez wsdl/serwis!
            return $@"
                <soapenv:Envelope xmlns:soapenv=""http://schemas.xmlsoap.org/soap/envelope/"" xmlns:pes=""http://msw.gov.pl/srp/v3_0/uslugi/pesel/"">
                    <soapenv:Header/>
                    <soapenv:Body>
                        <pes:wyszukajPodstawoweDaneOsoby>
                            <requestId>{SecurityElement.Escape(input.RequestId)}</requestId>
                            <kryteriaWyszukiwania>
                                <numerPesel>{SecurityElement.Escape(input.Pesel)}</numerPesel>
                            </kryteriaWyszukiwania>
                        </pes:wyszukajPodstawoweDaneOsoby>
                    </soapenv:Body>
                </soapenv:Envelope>";
        }


        public string BuildSearchRequestEnvelopeXml(SearchPersonInputModel input)
        {
            var sb = new StringBuilder();
            sb.Append("<soapenv:Envelope xmlns:soapenv=\"http://schemas.xmlsoap.org/soap/envelope/\" xmlns:pes=\"http://msw.gov.pl/srp/v3_0/uslugi/pesel/\">");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<pes:wyszukajPodstawoweDaneOsoby>");
            sb.Append($"<requestId>{SecurityElement.Escape(input.RequestId)}</requestId>");
            sb.Append("<kryteriaWyszukiwania>");
            if (!string.IsNullOrEmpty(input.Pesel))
            {
                sb.Append($"<numerPesel>{SecurityElement.Escape(input.Pesel)}</numerPesel>");
            }
            else
            {
                sb.Append("<kryteriumImienia>");
                sb.Append($"<imiePierwsze>{SecurityElement.Escape(input.ImiePierwsze)}</imiePierwsze>");
                sb.Append("<innyZapis>false</innyZapis>");
                sb.Append("<zakres>DANE_AKTUALNE</zakres>");
                sb.Append("</kryteriumImienia>");
                sb.Append("<kryteriumNazwiska>");
                sb.Append($"<nazwisko>{SecurityElement.Escape(input.Nazwisko)}</nazwisko>");
                sb.Append("<dowolneNazwisko>false</dowolneNazwisko>");
                sb.Append("<innyZapis>true</innyZapis>");
                sb.Append("<zakres>DANE_AKTUALNE</zakres>");
                sb.Append("</kryteriumNazwiska>");
            }
            sb.Append("</kryteriaWyszukiwania>");
            sb.Append("</pes:wyszukajPodstawoweDaneOsoby>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");
            return sb.ToString();

            
        }



        public wyszukajPodstawoweDaneOsobyResponse DeserializeSoapResponse(string responseXml)
        {
            var ns = "http://msw.gov.pl/srp/v3_0/uslugi/pesel/";
            var serializer = new XmlSerializer(typeof(wyszukajPodstawoweDaneOsobyResponse), ns);
            using var sr = new StringReader(responseXml);
            using var xr = XmlReader.Create(sr);
            // Przesuń do elementu właściwego, jeśli trzeba
            while (xr.Read())
            {
                if (xr.NodeType == XmlNodeType.Element && xr.LocalName == "wyszukajPodstawoweDaneOsobyResponse")
                    return (wyszukajPodstawoweDaneOsobyResponse)serializer.Deserialize(xr);
            }
            throw new InvalidOperationException("Brak odpowiedzi SOAP");
        }

        public BasicPersonDataResponse MapToDto(wyszukajPodstawoweDaneOsobyResponse response)
        {
            var basicPersonDataResponse = new BasicPersonDataResponse
            {
                Persons = new List<PersonData>()
            };

            if (response.znalezioneOsoby?.Length > 0)
            {
                    
                foreach (var person in response.znalezioneOsoby)
                {
                    basicPersonDataResponse.Persons.Add(new PersonData
                    {
                        OsobaId = person.osobaId,
                        NumerPesel = person.numerPesel,
                        CzyPeselAnulowany = person.czyPeselAnulowany,
                        ImiePierwsze = person.imiePierwsze,
                        ImieDrugie = person.imieDrugie,
                        ImionaKolejne = person.imionaKolejne,
                        Nazwisko = person.nazwisko,
                        NazwiskoRodowe = person.nazwiskoRodowe,
                        ObywatelstwoKod = person.obywatelstwo.kod,
                        ObywatelstwoNazwa = person.obywatelstwo.Value,
                        Plec = person.plec.ToString(),
                        MiejsceUrodzenia = person.miejsceUrodzenia,
                        DataUrodzenia = person.dataUrodzenia,
                        ImieMatki = person.imieMatki,
                        ImieOjca = person.imieOjca,
                        NazwiskoRodoweMatki = person.nazwiskoRodoweMatki,
                        NazwiskoRodoweOjca = person.nazwiskoRodoweOjca,
                        DanePobytuStalego = person.danePobytuStalego != null ? new PersonAddress
                        {

                            MiejscowoscKod = person.danePobytuStalego.miejscowoscDzielnica.kodTerytorialny,
                            MiejscowoscNazwa = person.danePobytuStalego.miejscowoscDzielnica.nazwaMiejscowosci,
                            UlicaCecha = person.danePobytuStalego.ulica.cecha,
                            UlicaNazwa = person.danePobytuStalego.ulica.nazwaPierwsza,
                            UlicaKodTeryt = person.danePobytuStalego.ulica.symbolUlicy,
                            NumerDomu = person.danePobytuStalego.numerDomu,
                            NumerLokalu = person.danePobytuStalego.numerLokalu,
                            KodPocztowy = person.danePobytuStalego.kodPocztowy,
                            GminaNazwa = person.danePobytuStalego.gmina.Value,
                            GminaKodTeryt = person.danePobytuStalego.gmina.kt,
                            WojewodztwoNazwa = person.danePobytuStalego.wojewodztwo.Value,
                            WojewodztwoKodTeryt = person.danePobytuStalego.wojewodztwo.woj,
                            DataOd = person.danePobytuStalego.dataOd,
                            DataDo = person.danePobytuStalego.dataDo,
                            Komentarz = person.danePobytuStalego.komentarz != null ? person.danePobytuStalego.komentarz : null

                        } : null,
                        DanePobytuCzasowego = person.danePobytuCzasowego != null ? new PersonAddress
                        {
                            MiejscowoscKod = person.danePobytuCzasowego.miejscowoscDzielnica.kodTerytorialny,
                            MiejscowoscNazwa = person.danePobytuCzasowego.miejscowoscDzielnica.nazwaMiejscowosci,
                            UlicaCecha = person.danePobytuCzasowego.ulica.cecha,
                            UlicaNazwa = person.danePobytuCzasowego.ulica.nazwaPierwsza,
                            UlicaKodTeryt = person.danePobytuCzasowego.ulica.symbolUlicy,
                            NumerDomu = person.danePobytuCzasowego.numerDomu,
                            NumerLokalu = person.danePobytuCzasowego.numerLokalu,
                            KodPocztowy = person.danePobytuCzasowego.kodPocztowy,
                            GminaNazwa = person.danePobytuCzasowego.gmina.Value,
                            GminaKodTeryt = person.danePobytuCzasowego.gmina.kt,
                            WojewodztwoNazwa = person.danePobytuCzasowego.wojewodztwo.Value,
                            WojewodztwoKodTeryt = person.danePobytuCzasowego.wojewodztwo.woj,
                            DataOd = person.danePobytuCzasowego.dataOd,
                            DataDo = person.danePobytuCzasowego.dataDo,
                            Komentarz = person.danePobytuCzasowego.komentarz != null ? person.danePobytuCzasowego.komentarz : null
                        } : null,
                        CzyZyje = person.czyZyje

                    });
                }
            }
            return basicPersonDataResponse;
                                
        }
    }
}
