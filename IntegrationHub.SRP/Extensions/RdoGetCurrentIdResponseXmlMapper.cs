using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Models;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntegrationHub.SRP.Extensions
{
    /// <summary>
    /// Mapuje XML odpowiedzi z SRP: udostepnijDaneAktualnegoDowoduPoPeselResponse na obiekt <see cref="GetCurrentIdByPeselResponse"/>.
    /// Działa na strukturze jak w przykładzie UdostepnijDaneAktualnegoDowoduPoPesel_RESPONSE.xml
    /// </summary>
    public static class RdoGetCurrentIdResponseXmlMapper
    {
        private static readonly XNamespace Ns2 = "http://msw.gov.pl/srp/v3_0/uslugi/dowody/";
        public static GetCurrentIdByPeselResponse Parse(string soapXml)
        {


            if (string.IsNullOrWhiteSpace(soapXml))
            {
                throw new ArgumentException("XML response is null or empty", nameof(soapXml));
            }


            var doc = XDocument.Parse(soapXml, LoadOptions.PreserveWhitespace);
            var wrapper = doc.Descendants(Ns2 + "udostepnijDaneAktualnegoDowoduPoPeselResponse").FirstOrDefault();
            if (wrapper == null)
                throw new InvalidOperationException("Nie znaleziono elementu udostepnijDaneAktualnegoDowoduPoPeselResponse w podanym XML.");

            // Wewnętrzny element (dowod) jest UNQUALIFIED (bez namespace'u)
            var dowodElement = wrapper.Element("dowod");
            if (dowodElement == null)
                return new GetCurrentIdByPeselResponse();




            var dowod = new DowodOsobisty
            {
                DataWaznosci = ConvertDateString(ValueOf(dowodElement, "dataWaznosci")),
                DataWydania = ConvertDateString(ValueOf(dowodElement, "dataWydania")),
                ZdjecieCzarnoBiale = ValueOf(dowodElement, "zdjecieCzarnoBiale"),
                ZdjecieKolorowe = ValueOf(dowodElement, "zdjecieKolorowe"),
                statusDokumentu = ValueOf(dowodElement, "statusDokumentu"),
                statusWarstwyEdo = ValueOf(dowodElement, "statusWarstwyEdo"),
                obywatelstwo = ValueOf(dowodElement, "obywatelstwo"),
                idDowodu = ValueOf(dowodElement, "idDowodu"),
                kodTerytUrzeduWydajacego = ValueOf(dowodElement, "kodTerytUrzeduWydajacego"),
                nazwaUrzeduWydajacego = ValueOf(dowodElement, "nazwaUrzeduWydaja")

            };

            var seriaINumerElement = dowodElement.Element("seriaINumer");
            if (seriaINumerElement != null)
            {
                dowod.SeriaINumer = new SeriaINumerDokumentuTozsamosci
                {
                    seriaDokumentuTozsamosci = ValueOf(seriaINumerElement, "seriaDokumentuTozsamosci"),
                    numerDokumentuTozsamosci = ValueOf(seriaINumerElement, "numerDokumentuTozsamosci")
                };
            }
            var daneOsoboweElement = dowodElement.Element("daneOsobowe");
            if(daneOsoboweElement != null)
            {
                var imieElement = daneOsoboweElement.Element("imie");
                Imiona? imiona = null;
                if (imieElement != null)
                {
                    imiona = new Imiona
                    {
                        imiePierwsze = ValueOf(imieElement, "imiePierwsze"),
                        imieDrugie = ValueOf(imieElement, "imieDrugie")
                    };
                }
                var nazwiskoElement = daneOsoboweElement.Element("nazwisko");
                Nazwisko? nazwisko = null;
                if (nazwiskoElement != null)
                {
                    nazwisko = new Nazwisko
                    {
                        czlonPierwszy = ValueOf(nazwiskoElement, "czlonPierwszy"),
                        czlonDrugi = ValueOf(nazwiskoElement, "czlonDrugi")
                    };
                }

                var nazwiskoRodoweElement = daneOsoboweElement.Element("nazwiskoRodowe");
                

                dowod.DaneOsobowe = new DaneOsobowe
                {
                    imie = imiona,
                    nazwisko = nazwisko,
                    pesel = ValueOf(daneOsoboweElement, "pesel"),
                    idOsoby = ValueOf(daneOsoboweElement, "idOsoby"),
                    nazwiskoRodowe = ValueOf(nazwiskoRodoweElement, "nazwisko")
                    
                };

                var podstawoweDaneUrodzeniaElement = dowodElement.Element("podstawoweDaneUrodzeniaDowod");
                dowod.daneUrodzenia = new PodstawoweDaneUrodzenia
                {
                    dataUrodzenia = ConvertDateString(ValueOf(podstawoweDaneUrodzeniaElement, "dataUrodzenia")),
                    miejsceUrodzenia = ValueOf(podstawoweDaneUrodzeniaElement, "miejsceUrodzenia"),
                    imieOjca = ValueOf(podstawoweDaneUrodzeniaElement, "imieOjca"),
                    imieMatki = ValueOf(podstawoweDaneUrodzeniaElement, "imieMatki"),
                    plec = ValueOf(podstawoweDaneUrodzeniaElement, "plec")
                };

                var daneWystawcyElement = dowodElement.Element("daneWystawcy");
                if (daneWystawcyElement != null)
                {
                    dowod.daneWystawcy = new DaneWystawcyDowodu
                    {
                        idOrganu = ValueOf(daneWystawcyElement, "idOrgan"),
                        nazwaWystawcy = ValueOf(daneWystawcyElement, "nazwaWystawcyZDowodu")
                    };
                }

                
            }


            return new GetCurrentIdByPeselResponse
            {
                Dowod = dowod
            };


        }


        private static String? ConvertDateString(string? yyyymmdd)
        {
            if (string.IsNullOrWhiteSpace(yyyymmdd) || yyyymmdd!.Length != 8)
                return null;
            var yyyy = yyyymmdd.Substring(0, 4);
            var mm = yyyymmdd.Substring(4, 2);
            var dd = yyyymmdd.Substring(6, 2);
            return $"{yyyy}-{mm}-{dd}";
        }

        private static string? ValueOf(XElement? parent, string localName)
        {
            var el = Child(parent, localName);
            var v = el?.Value?.Trim();
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }

        private static XElement? Child(XElement? parent, string localName) =>
        parent?.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));

    }
}
