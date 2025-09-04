using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.PESEL.SoapClient;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Linq;

namespace IntegrationHub.SRP.Helpers
{
    public static class RequestEnvelopeHelper
    {

        public static KryteriaWyszukiwaniaOsob GetCriteriaFromRequest(SearchPersonRequest body)
        {

            static string UpperPl(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToUpper(new System.Globalization.CultureInfo("pl-PL"));
            static string X(string? s) => System.Security.SecurityElement.Escape(s ?? string.Empty);

            var hasPesel = !string.IsNullOrWhiteSpace(body.Pesel);
            var hasImiePierwsze = !string.IsNullOrWhiteSpace(body.ImiePierwsze);
            var hasImieDrugie = !string.IsNullOrWhiteSpace(body.ImieDrugie);
            var hasNazwisko = !string.IsNullOrWhiteSpace(body.Nazwisko);
            var hasDataUrodzenia = !string.IsNullOrWhiteSpace(body.DataUrodzenia);
            var hasImieMatki = !string.IsNullOrWhiteSpace(body.ImieMatki);
            var hasImieOjca = !string.IsNullOrWhiteSpace(body.ImieOjca);
            var hasKryteriumUrodzenia = hasDataUrodzenia || hasImieMatki || hasImieOjca;
            var hasKryteriumImienia = hasImiePierwsze || hasImieDrugie;

            var kryteria = new KryteriaWyszukiwaniaOsob();
            if (hasPesel)
            {
                kryteria.numerPesel = X(body.Pesel!.Trim());
            }

            if (hasKryteriumImienia)
            {
                kryteria.kryteriumImienia = new KryteriumImienia();
                if (hasImiePierwsze)
                    kryteria.kryteriumImienia.imiePierwsze = UpperPl(body.ImiePierwsze!.Trim());
                if (hasImieDrugie)
                    kryteria.kryteriumImienia.imieDrugie = UpperPl(body.ImieDrugie!.Trim());
                kryteria.kryteriumImienia.innyZapis = false;
                kryteria.kryteriumImienia.zakres = ZakresWyszukiwania.DANE_AKTUALNE;
            }

            if (hasNazwisko)
            {
                kryteria.kryteriumNazwiska = new KryteriumNazwiska();
                kryteria.kryteriumNazwiska.nazwisko = UpperPl(body.Nazwisko!.Trim());
                kryteria.kryteriumNazwiska.dowolneNazwisko = true;
                kryteria.kryteriumNazwiska.innyZapis = true;
                kryteria.kryteriumNazwiska.zakres = ZakresWyszukiwania.DANE_AKTUALNE;
            }
            if (hasKryteriumUrodzenia)
            {
                kryteria.kryteriumDanychUrodzenia = new KryteriumDanychUrodzenia();
                if (hasDataUrodzenia)
                    kryteria.kryteriumDanychUrodzenia.dataUrodzenia = new KryteriumDat() { Item = body.DataUrodzenia };
                    
                if (hasImieMatki)
                    kryteria.kryteriumDanychUrodzenia.imieMatki = UpperPl(body.ImieMatki!.Trim());
                if (hasImieOjca)
                    kryteria.kryteriumDanychUrodzenia.imieOjca = UpperPl(body.ImieOjca!.Trim());
                kryteria.kryteriumDanychUrodzenia.zakres = ZakresWyszukiwania.DANE_AKTUALNE;
            }
            return kryteria;
        }

        public static string PrepareSearchPersonBaseDataEnvelope(SearchPersonRequest body, string requestId)
        {
            string UpperPl(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToUpper(new System.Globalization.CultureInfo("pl-PL"));
            string X(string? s) => System.Security.SecurityElement.Escape(s ?? string.Empty);

            var hasPesel = !string.IsNullOrWhiteSpace(body.Pesel);
            var sb = new System.Text.StringBuilder();


            sb.Append("<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:pes='http://msw.gov.pl/srp/v3_0/uslugi/pesel/'>");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<pes:wyszukajPodstawoweDaneOsoby>");
            sb.Append($"<requestId>{X(requestId)}</requestId>");
            sb.Append("<kryteriaWyszukiwania>");


            if (hasPesel)
            {
                sb.Append($"<numerPesel>{X(body.Pesel!.Trim())}</numerPesel>");
            }

            // <kryteriumImienia>
            var hasImiona = !string.IsNullOrWhiteSpace(body.ImiePierwsze) || !string.IsNullOrWhiteSpace(body.ImieDrugie);
            if (hasImiona)
            {
                sb.Append("<kryteriumImienia>");
                if (!string.IsNullOrWhiteSpace(body.ImiePierwsze))
                    sb.Append($"<imiePierwsze>{X(UpperPl(body.ImiePierwsze))}</imiePierwsze>");
                if (!string.IsNullOrWhiteSpace(body.ImieDrugie))
                    sb.Append($"<imieDrugie>{X(UpperPl(body.ImieDrugie))}</imieDrugie>");


                sb.Append($"<innyZapis>false</innyZapis>");
                sb.Append("<zakres>DANE_AKTUALNE</zakres>");
                sb.Append("</kryteriumImienia>");
            }

            // <kryteriumNazwiska>
            var hasNazwisko = !string.IsNullOrWhiteSpace(body.Nazwisko);
           if (hasNazwisko)
            {
                sb.Append("<kryteriumNazwiska>");
                if (!string.IsNullOrWhiteSpace(body.Nazwisko))
                    sb.Append($"<nazwisko>{X(UpperPl(body.Nazwisko))}</nazwisko>");
                sb.Append($"<dowolneNazwisko>true</dowolneNazwisko>");
                sb.Append($"<innyZapis>true</innyZapis>");
                sb.Append("<zakres>DANE_AKTUALNE</zakres>");
                sb.Append("</kryteriumNazwiska>");
            }

            // <kryteriumDanychUrodzenia>
            var hasBirthBlock = !string.IsNullOrWhiteSpace(body.DataUrodzenia) || !string.IsNullOrWhiteSpace(body.ImieMatki) || !string.IsNullOrWhiteSpace(body.ImieOjca);
            if (hasBirthBlock)
            {
                sb.Append("<kryteriumDanychUrodzenia>");
                if (!string.IsNullOrWhiteSpace(body.DataUrodzenia))
                {
                    sb.Append("<dataUrodzenia>");
                    sb.Append($"<kryteriumDaty>{X(body.DataUrodzenia)}</kryteriumDaty>");
                    sb.Append("</dataUrodzenia>");
                }
                if (!string.IsNullOrWhiteSpace(body.ImieMatki))
                    sb.Append($"<imieMatki>{X(UpperPl(body.ImieMatki))}</imieMatki>");
                if (!string.IsNullOrWhiteSpace(body.ImieOjca))
                    sb.Append($"<imieOjca>{X(UpperPl(body.ImieOjca))}</imieOjca>");


                sb.Append("<zakres>DANE_AKTUALNE</zakres>");
                sb.Append("</kryteriumDanychUrodzenia>");
            }


            sb.Append("</kryteriaWyszukiwania>");
            sb.Append("</pes:wyszukajPodstawoweDaneOsoby>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();

        }

        /// <summary>
        ///Zwraca SOAP Envelope do wyszukiwania osoby w SRP (PESEL). 
        /// Wymagane: podaj <b>PESEL</b> albo zestaw: <b>Nazwisko</b> i <b>Imię</b>.
        /// Pole <c>dataUrodzenia</c> oczekuje formatu <c>yyyyMMdd</c> lub <c>yyyy-MM-dd</c>.
        /// Można podać dokładną datę urodzenia (pole dataUrodzenia) albo zakres dat (dataUrodzeniaOd, dataUrodzeniaDo).
        /// Nie można podać obu naraz (jeśli podano dokładną datę, ignorujemy zakres).
        /// </summary>
        public static string PrepareSearchPersonEnvelope(SearchPersonRequest body, string requestId)
        {
            string UpperPl(string? s) => string.IsNullOrWhiteSpace(s) ? "" : s.Trim().ToUpper(new System.Globalization.CultureInfo("pl-PL"));
            string X(string? s) => System.Security.SecurityElement.Escape(s ?? string.Empty);

            var hasPesel = !string.IsNullOrWhiteSpace(body.Pesel);

            var sb = new System.Text.StringBuilder();


            sb.Append("<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:pes='http://msw.gov.pl/srp/v3_0/uslugi/pesel/'>");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<pes:wyszukajOsoby>");
            sb.Append($"<requestId>{X(requestId)}</requestId>");
            sb.Append("<kryteriaWyszukiwania>");


            if (hasPesel)
            {
                sb.Append($"<numerPesel>{X(body.Pesel!.Trim())}</numerPesel>");
            }

            // <kryteriumImienia>
            var hasImiona = !string.IsNullOrWhiteSpace(body.ImiePierwsze) || !string.IsNullOrWhiteSpace(body.ImieDrugie);
            if (hasImiona)
            {
                sb.Append("<kryteriumImienia>");
                if (!string.IsNullOrWhiteSpace(body.ImiePierwsze))
                    sb.Append($"<imiePierwsze>{X(UpperPl(body.ImiePierwsze))}</imiePierwsze>");
                if (!string.IsNullOrWhiteSpace(body.ImieDrugie))
                    sb.Append($"<imieDrugie>{X(UpperPl(body.ImieDrugie))}</imieDrugie>");


                sb.Append($"<innyZapis>false</innyZapis>");
                sb.Append("<zakres>DANE_AKTUALNE</zakres>");
                sb.Append("</kryteriumImienia>");
            }

            // <kryteriumNazwiska>
            var hasNazwisko = !string.IsNullOrWhiteSpace(body.Nazwisko);
            if (hasNazwisko)
            {
                sb.Append("<kryteriumNazwiska>");
                if (!string.IsNullOrWhiteSpace(body.Nazwisko))
                    sb.Append($"<nazwisko>{X(UpperPl(body.Nazwisko))}</nazwisko>");
                sb.Append($"<dowolneNazwisko>true</dowolneNazwisko>");
                sb.Append($"<innyZapis>true</innyZapis>");
                sb.Append("<zakres>DANE_AKTUALNE</zakres>");
                sb.Append("</kryteriumNazwiska>");
            }

            // <kryteriumDanychUrodzenia>
            var hasDataUrodzenia = !string.IsNullOrWhiteSpace(body.DataUrodzenia);
            var hasDataUrodzeniaOd = !string.IsNullOrWhiteSpace(body.DataUrodzeniaOd);
            var hasDataUrodzeniaDo = !string.IsNullOrWhiteSpace(body.DataUrodzeniaDo);
            var hasImieOjca = !string.IsNullOrWhiteSpace(body.ImieOjca);
            var hasImieMatki = !string.IsNullOrWhiteSpace(body.ImieMatki);
            var hasKryteriumDatyUrodzenia = hasDataUrodzenia || hasDataUrodzeniaOd || hasDataUrodzeniaDo;
            var hasKryteriumDanychUrodzenia = hasKryteriumDatyUrodzenia || hasImieMatki || hasImieOjca;

            if (hasKryteriumDanychUrodzenia)
            {
                sb.Append("<kryteriumDanychUrodzenia>");

                //W przpadku daty urodzenia:
                //- można podać dokładną datę urodzenia (pole dataUrodzenia) albo zakres dat (dataUrodzeniaOd, dataUrodzeniaDo)
                //- nie można podać obu naraz (jeśli podano dokładną datę, ignorujemy zakres)
                if (hasKryteriumDatyUrodzenia)
                {
                    sb.Append("<dataUrodzenia>");
                    if (hasDataUrodzenia)
                        sb.Append($"<kryteriumDaty>{X(body.DataUrodzenia)}</kryteriumDaty>");
                    else 
                    {
                        sb.Append("<kryteriumPrzedzialDat>");
                        if (hasDataUrodzeniaOd)
                            sb.Append($"<dataOd>{X(body.DataUrodzeniaOd)}</dataOd>");
                        if (hasDataUrodzeniaDo)
                            sb.Append($"<dataDo>{X(body.DataUrodzeniaDo)}</dataDo>");
                        sb.Append("</kryteriumPrzedzialDat>");
                    }
                    sb.Append("</dataUrodzenia>");
                }
                
                if (!string.IsNullOrWhiteSpace(body.ImieMatki))
                    sb.Append($"<imieMatki>{X(UpperPl(body.ImieMatki))}</imieMatki>");
                if (!string.IsNullOrWhiteSpace(body.ImieOjca))
                    sb.Append($"<imieOjca>{X(UpperPl(body.ImieOjca))}</imieOjca>");


                sb.Append("<zakres>DANE_AKTUALNE</zakres>");
                sb.Append("</kryteriumDanychUrodzenia>");
            }


            sb.Append("</kryteriaWyszukiwania>");
            sb.Append("</pes:wyszukajOsoby>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");

            return sb.ToString();

        }


        public static string PrepareShareIdCardRequestEnvelope(GetIdCardRequest body)
        {
            string X(string? s) => System.Security.SecurityElement.Escape(s ?? string.Empty);
            var sb = new System.Text.StringBuilder();
            sb.Append("<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:dow='http://msw.gov.pl/srp/v3_0/uslugi/dowody/'>");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<dow:udostepnijDaneAktualnychDowodowPoPesel>");
            sb.Append("<listaNumerowPesel>");
            foreach (var pesel in body.NumeryPesel)
            {
                sb.Append($"<numerPesel>{X(pesel.Trim())}</numerPesel>");
            }
            sb.Append("</listaNumerowPesel>");
            sb.Append("</dow:udostepnijDaneAktualnychDowodowPoPesel>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");
            return sb.ToString();
        }


        public static string PrepareGetCurrentPhotoRequestEnvelope(GetCurrentPhotoRequest body, string requestId)
        {
            var sb = new StringBuilder();
            sb.Append("<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:dow='http://msw.gov.pl/srp/v3_0/uslugi/dowody/'>");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<dow:udostepnijAktualneZdjecie>");
            sb.Append($"<pesel>{X(body?.Pesel)}</pesel>");
            sb.Append($"<idOsoby>{X(body?.IdOsoby)}</idOsoby>");
            sb.Append($"<requestId>{X(requestId)}</requestId>");
            sb.Append("</dow:udostepnijAktualneZdjecie>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");
            return sb.ToString();
        }


        public static string PrepareGetPersonEnvelope(GetPersonRequest body, string requestId)
        {
            var sb = new StringBuilder();
            sb.Append("<soapenv:Envelope xmlns:soapenv='http://schemas.xmlsoap.org/soap/envelope/' xmlns:pes='http://msw.gov.pl/srp/v3_0/uslugi/pesel/'>");
            sb.Append("<soapenv:Header/>");
            sb.Append("<soapenv:Body>");
            sb.Append("<pes:udostepnijAktualneDaneOsobyPoId>");
            sb.Append($"<requestId>{X(requestId)}</requestId>");
            sb.Append($"<idOsoby>{X(body?.OsobaId)}</idOsoby>");
            sb.Append("</pes:udostepnijAktualneDaneOsobyPoId>");
            sb.Append("</soapenv:Body>");
            sb.Append("</soapenv:Envelope>");
            return sb.ToString();
        }



        /// <summary>
        /// Próba wykrycia i sparsowania SOAP Fault z surowego XML (SOAP 1.1/1.2). Zwraca true, jeśli znaleziono Fault.
        /// W detail próbujemy wyciągnąć typowe pola SRP: "kod", "opis" i (jeśli dostępne) "opis techniczny".
        /// </summary>
        public static bool TryParseSoapFault(string xml, out SoapFaultResponse? fault)
        {
            fault = null;
            if (string.IsNullOrWhiteSpace(xml)) return false;

            try
            {
                var doc = XDocument.Parse(xml, LoadOptions.PreserveWhitespace);
                XNamespace soap11 = "http://schemas.xmlsoap.org/soap/envelope/";
                XNamespace soap12 = "http://www.w3.org/2003/05/soap-envelope";

                // SOAP 1.1
                var fault11 = doc.Descendants(soap11 + "Fault").FirstOrDefault();
                if (fault11 != null)
                {
                    var code = (string?)fault11.Element("faultcode");
                    var reason = (string?)fault11.Element("faultstring") ?? (string?)fault11.Element("faultreason");
                    var detail = fault11.Element("detail");

                    var (dCode, dOpis, dOpisTech) = ExtractDetail(detail);

                    fault = new SoapFaultResponse(
                        code ?? "Server",
                        reason ?? "SOAP Fault",
                        dCode,
                        dOpis,
                        dOpisTech
                    );
                    return true;
                }

                // SOAP 1.2
                var fault12 = doc.Descendants(soap12 + "Fault").FirstOrDefault();
                if (fault12 != null)
                {
                    var code = (string?)fault12.Element(soap12 + "Code")?.Element(soap12 + "Value");
                    var reason = (string?)fault12.Element(soap12 + "Reason")?.Elements(soap12 + "Text").FirstOrDefault();
                    var detail = fault12.Element(soap12 + "Detail");

                    var (dCode, dOpis, dOpisTech) = ExtractDetail(detail);

                    fault = new SoapFaultResponse(
                        code ?? "Receiver",
                        reason ?? "SOAP Fault",
                        dCode,
                        dOpis,
                        dOpisTech
                    );
                    return true;
                }
            }
            catch
            {
                // best-effort – nie wywracamy procesu parsowania odpowiedzi, gdy XML jest niepoprawny
                return false;
            }

            return false;
        }

        private static (string? kod, string? opis, string? opisTech) ExtractDetail(XElement? detail)
        {
            if (detail == null) return (null, null, null);

            string? kod = null;
            string? opis = null;
            string? opisTech = null;

            // Szukamy po LocalName (różne przestrzenie nazw w SRP)
            var eKod = detail.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("kod", StringComparison.OrdinalIgnoreCase));
            var eOpis = detail.Descendants().FirstOrDefault(e => e.Name.LocalName.Equals("opis", StringComparison.OrdinalIgnoreCase));

            // Często "opis techniczny" nie ma jednolitej nazwy – spróbuj złapać popularne warianty
            var eOpisTech = detail.Descendants().FirstOrDefault(e =>
                e.Name.LocalName.Equals("opisTechniczny", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("opis_techniczny", StringComparison.OrdinalIgnoreCase) ||
                e.Name.LocalName.Equals("technicalDescription", StringComparison.OrdinalIgnoreCase));

            if (eKod != null) kod = eKod.Value;
            if (eOpis != null) opis = eOpis.Value;
            if (eOpisTech != null) opisTech = eOpisTech.Value;

            // Jeżeli nie mamy wyraźnego pola technicznego – zapisz cały detail jako techniczny ślad
            if (opisTech == null)
                opisTech = detail.ToString(SaveOptions.DisableFormatting);

            return (kod, opis, opisTech);
        }

        private static string X(string? s) => System.Security.SecurityElement.Escape((s ?? string.Empty).Trim());

    }
}
