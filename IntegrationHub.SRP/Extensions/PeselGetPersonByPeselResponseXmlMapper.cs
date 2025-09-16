using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Models;
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntegrationHub.SRP.Extensions
{
    /// <summary>
    /// Mapuje XML odpowiedzi z SRP: udostepnijDaneAktualnegoDowoduPoPeselResponse na obiekt <see cref="GetPersonByPeselResponse"/>.
    /// Działa na strukturze jak w przykładzie UdostepnijDaneAktualnegoDowoduPoPesel_RESPONSE.xml
    /// </summary>
    public static class PeselGetPersonByPeselResponseXmlMapper
    {
        private static readonly XNamespace Ns2 = "http://msw.gov.pl/srp/v3_0/uslugi/pesel/";
        public static GetPersonByPeselResponse Parse(string soapXml)
        {


            if (string.IsNullOrWhiteSpace(soapXml))
            {
                throw new ArgumentException("XML response is null or empty", nameof(soapXml));
            }


            var doc = XDocument.Parse(soapXml, LoadOptions.PreserveWhitespace);
            var wrapper = doc.Descendants(Ns2 + "udostepnijAktualneDaneOsobyPoPeselResponse").FirstOrDefault();
            if (wrapper == null)
                throw new InvalidOperationException("Nie znaleziono elementu udostepnijAktualneDaneOsobyPoPeselResponse w podanym XML.");

            // Wewnętrzny element (dowod) jest UNQUALIFIED (bez namespace'u)
            var daneOsobyElement = wrapper.Element("daneOsoby");
            if (daneOsobyElement == null)
                return new GetPersonByPeselResponse();

            var daneOsoby = new Osoba
            {
                CzyAnulowano = ValueOf(daneOsobyElement, "czyAnulowano") == "true",
                IdOsoby = ValueOf(daneOsobyElement, "idOsoby"),
                NumerPesel = ValueOf(daneOsobyElement, "pesel"),
                DataAktualizacji = ValueOf(daneOsobyElement, "dataAktualizacji")
            };

            var daneDowoduElement = daneOsobyElement.Element("daneDowoduOsobistego");
            daneOsoby.DaneDowoduOsobistego = new DaneDowoduOsobistego
            {
                DataWaznosci = ConvertDateString(ValueOf(daneDowoduElement, "dataWaznosci")),
                SeriaINumer = ValueOf(daneDowoduElement, "seriaINumer"),
                Wystawca = new Organ
                {
                    KodTerytorialny = ValueOf(Child(daneDowoduElement, "wystawca"), "kodTerytorialny"),
                    RodzajOrganu = ValueOf(Child(daneDowoduElement, "wystawca"), "rodzajOrganu")
                }
            };

            var daneImionElement = daneOsobyElement.Element("daneImion");
            daneOsoby.Imiona = new DaneImion
            {
                ImiePierwsze = ValueOf(daneImionElement, "imiePierwsze"),
                ImieDrugie = ValueOf(daneImionElement, "imieDrugie")
            };

            var daneKrajowZamieszkaniaElement = daneOsobyElement.Element("daneKrajowZamieszkania"); 
            daneOsoby.KrajZamieszkania = new DaneKrajowZamieszkania
            {
                KrajZamieszkania = ValueOf(daneKrajowZamieszkaniaElement, "krajZamieszkania"),
                
            };

            var daneNazwiskaElement = daneOsobyElement.Element("daneNazwiska");
            daneOsoby.Nazwiska = new DaneNazwiska
            {
                Nazwisko = ValueOf(daneNazwiskaElement, "nazwisko"),
                NazwiskoRodowe = ValueOf(Child(daneNazwiskaElement,"nazwiskoRodowe"),"nazwisko")
            };

            var daneObywatelstwaElement = daneOsobyElement.Element("daneObywatelstwa");
            daneOsoby.Obywatelstwo = ValueOf(daneObywatelstwaElement, "obywatelstwo");

            var danePaszportuElement = daneOsobyElement.Element("danePaszportu");
            if (danePaszportuElement != null)
            {
                daneOsoby.Paszport = new DanePaszportu
                {
                    SeriaINumer = ValueOf(danePaszportuElement, "seriaINumer"),
                    DataWaznosci = ConvertDateString(ValueOf(danePaszportuElement, "dataWaznosci")),
                    
                };
            }

            var danePobytuStalegoElement = daneOsobyElement.Element("danePobytuStalego");
            if (danePobytuStalegoElement != null)
            {
                daneOsoby.DanePobytuStalego = new DanePobytu
                {
                    Miejscowosc = ValueOf(Child(danePobytuStalegoElement,"miejscowoscDzielnica"), "nazwaMiejscowosci"),
                    UlicaCecha = ValueOf(Child(danePobytuStalegoElement, "ulica"), "cecha"),
                    UlicaNazwa = ValueOf(Child(danePobytuStalegoElement, "ulica"),"nazwaPierwsza"),
                    NumerDomu = ValueOf(danePobytuStalegoElement, "numerDomu"),
                    NumerLokalu = ValueOf(danePobytuStalegoElement, "numerLokalu"),
                    Gmina = ValueOf(danePobytuStalegoElement, "gmina"),
                    Wojewodztwo = ValueOf(danePobytuStalegoElement, "wojewodztwo"),
                    DataOd = ConvertDateString(ValueOf(danePobytuStalegoElement, "dataOd"))
                };
            }

            var danePobytuCzasowegoElement = daneOsobyElement.Element("danePobytuCzasowego");
            if (danePobytuCzasowegoElement != null)
            {
                daneOsoby.DanePobytuCzasowego = new DanePobytu
                {
                    Miejscowosc = ValueOf(Child(danePobytuCzasowegoElement, "miejscowoscDzielnica"), "nazwaMiejscowosci"),
                    UlicaCecha = ValueOf(Child(danePobytuCzasowegoElement, "ulica"), "cecha"),
                    UlicaNazwa = ValueOf(Child(danePobytuCzasowegoElement, "ulica"), "nazwaPierwsza"),
                    NumerDomu = ValueOf(danePobytuCzasowegoElement, "numerDomu"),
                    NumerLokalu = ValueOf(danePobytuCzasowegoElement, "numerLokalu"),
                    Gmina = ValueOf(danePobytuCzasowegoElement, "gmina"),
                    Wojewodztwo = ValueOf(danePobytuCzasowegoElement, "wojewodztwo"),
                    DataOd = ConvertDateString(ValueOf(danePobytuCzasowegoElement, "dataOd"))
                };
            }

            var daneStanuCywilnegoElement = daneOsobyElement.Element("daneStanuCywilnego");
            if (daneStanuCywilnegoElement != null)
            {
                daneOsoby.StanCywilny = new DaneStanuCywilnego
                {
                    DataZawarcia = ConvertDateString(ValueOf(daneStanuCywilnegoElement, "dataZawarcia")),
                    NumerAktu = ValueOf(daneStanuCywilnegoElement, "numerAktu"),
                    StanCywilny = ValueOf(daneStanuCywilnegoElement, "stanCywilny"),
                    CzyZmienianoPlec = ValueOf(daneStanuCywilnegoElement, "czyZmienianoPlec") == "true"
                };
                var wspolmalzonekElement = daneStanuCywilnegoElement.Element("wspolmalzonek");
                if (wspolmalzonekElement != null)
                {
                    daneOsoby.StanCywilny.Wspolmalzonek = new Wspolmalzonek
                    {
                        Imie = ValueOf(wspolmalzonekElement, "imie"),
                        NazwiskoRodowe = ValueOf(wspolmalzonekElement, "nazwiskoRodowe"),
                        NumerPesel = ValueOf(wspolmalzonekElement, "numerPesel")
                        
                    };
                }


            }

            var daneUrodzeniaElement = daneOsobyElement.Element("daneUrodzenia");
            daneOsoby.Urodzenie = new DaneUrodzenia
            {
                DataUrodzenia = ConvertDateString(ValueOf(daneUrodzeniaElement, "dataUrodzenia")),
                ImieMatki = ValueOf(daneUrodzeniaElement, "imieMatki"),
                ImieOjca = ValueOf(daneUrodzeniaElement, "imieOjca"),
                KrajUrodzenia = ValueOf(daneUrodzeniaElement, "krajUrodzenia"),
                MiejsceUrodzenia = ValueOf(Child(daneUrodzeniaElement, "miejsceUrodzenia"),"nazwaMiejscowosci"),
                NazwiskoRodoweMatki = ValueOf(daneUrodzeniaElement, "nazwiskoRodoweMatki"),
                NazwiskoRodoweOjca = ValueOf(daneUrodzeniaElement, "nazwiskoRodoweOjca"),
                NumerAktu = ValueOf(daneUrodzeniaElement, "numerAktu"),
                Plec = ValueOf(daneUrodzeniaElement, "plec")

            };

            return new GetPersonByPeselResponse
            {
                daneOsoby= daneOsoby
            };


        }



        private static string? ConvertDateString(string? input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return null;

            input = input.Trim();

            // przypadek yyyyMMdd
            if (input.Length == 8 && input.All(char.IsDigit))
            {
                var yyyy = input.Substring(0, 4);
                var mm = input.Substring(4, 2);
                var dd = input.Substring(6, 2);
                return $"{yyyy}-{mm}-{dd}";
            }

            // przypadek yyyy-MM-dd
            if (input.Length == 10 && input[4] == '-' && input[7] == '-')
            {
                if (DateTime.TryParseExact(input, "yyyy-MM-dd",
                                           System.Globalization.CultureInfo.InvariantCulture,
                                           System.Globalization.DateTimeStyles.None,
                                           out var dt))
                {
                    return dt.ToString("yyyy-MM-dd");
                }
            }

            return null;
        }


        private static string? ValueOf(XElement? parent, string localName)
        {
            var el = Child(parent, localName);
            var v = el?.Value?.Trim();
            return string.IsNullOrWhiteSpace(v) ? null : v;
        }

        private static XElement? Child(XElement? parent, string localName) =>
        parent?.Elements().FirstOrDefault(e => e.Name.LocalName.Equals(localName, StringComparison.OrdinalIgnoreCase));


        public static class DateParser
        {
            private static readonly string[] Formats =
            {
        "yyyy-MM-ddTHH:mm:ss.fff",
        "yyyy-MM-ddTHH:mm:ss" // czasami może nie być milisekund
    };

            /// <summary>
            /// Konwertuje string daty w formacie SRP (np. "2023-10-26T14:06:20.297") na DateTime?.
            /// </summary>
            public static DateTime? ParseSrpDateTime(string? value)
            {
                if (string.IsNullOrWhiteSpace(value))
                    return null;

                if (DateTime.TryParseExact(value, Formats, CultureInfo.InvariantCulture,
                                           DateTimeStyles.AssumeLocal | DateTimeStyles.AdjustToUniversal,
                                           out var dt))
                {
                    return dt;
                }

                return null;
            }
        }
    }
}
