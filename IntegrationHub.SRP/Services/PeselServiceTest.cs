using IntegrationHub.Common.Configs;
using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.Helpers;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Helpers;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using static IntegrationHub.SRP.Services.SrpServiceCommon;

namespace IntegrationHub.SRP.Services
{
    public sealed class PeselServiceTest : IPeselService
    {
        private readonly SrpConfig _srp;
        private readonly ISrpSoapInvoker _invoker;
        private readonly ILogger<PeselServiceTest> _logger;
        private readonly string _testDataDir;

        public PeselServiceTest(IOptions<SrpConfig> srpConfig, ISrpSoapInvoker invoker, ILogger<PeselServiceTest> logger,IHostEnvironment env)
        {
            _srp = srpConfig.Value;
            _invoker = invoker;
            _logger = logger;
            _testDataDir = System.IO.Path.Combine(env.ContentRootPath, "TestData","SRP");// <contentRoot>\TestData\SRP
        }

        public async Task<ProxyResponse<SearchPersonResponse>> SearchBasePersonDataAsync(SearchPersonRequest body, string? requestId = null, CancellationToken ct = default)
        {
            throw new NotImplementedException();
        }

        public async Task<ProxyResponse<GetPersonResponse>> GetPersonAsync(GetPersonRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            

            try
            {

                if (string.IsNullOrWhiteSpace(body.OsobaId))
                {
                    throw new ArgumentException("Brak wymaganego parametru: osobaId");
                }

                var envelope = RequestEnvelopeHelper.PrepareGetPersonEnvelope(body, requestId);
                var result = await _invoker.InvokeAsync(_srp.PeselShareServiceUrl, SrpSoapActions.Pesel_UdostepnijAktualneDaneOsobyPoId,
                                                        envelope, requestId, ct);
                if (result.Fault is not null)
                {
                    var msg = result.Fault.DetailOpis ?? result.Fault.FaultString;
                    if (!string.IsNullOrWhiteSpace(result.Fault.DetailOpisTechniczny))
                        msg += $"; {result.Fault.DetailOpisTechniczny}";

                    return ProxyResponseError<GetPersonResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, msg);
                }

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                {
                    return ProxyResponseError<GetPersonResponse>(requestId, result.StatusCode,
                        ProxyStatus.TechnicalError, $"HTTP {(int)result.StatusCode}");
                }

                // TODO: Zmapuj response XML -> GetPersonResponse (teraz wkładamy RAW w NumerPesel)
                var responseObj = new GetPersonResponse();
                responseObj.NumerPesel = result.Body;

                return new ProxyResponse<GetPersonResponse>
                {
                    RequestId = requestId,
                    Data = responseObj,
                    Source = "SRP",
                    Status = ProxyStatus.Success,
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (ArgumentException aex)
            {
                return ProxyResponseError<GetPersonResponse>(requestId, HttpStatusCode.BadRequest,
                    ProxyStatus.BusinessError, aex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd GetPerson, RequestId: {RequestId}", requestId);
                return ProxyResponseError<GetPersonResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, ex.Message);
            }
        }

        private static ProxyResponse<T> ProxyResponseError<T>(string requestId, HttpStatusCode code, ProxyStatus status, string message)
        {
            return new ProxyResponse<T>
            {
                RequestId = requestId,
                Source = "SRP",
                Status = status,
                StatusCode = (int)code,
                ErrorMessage = message
            };
        }

        /// <summary>
        /// Wysyłanie zapytania o wyszukanie osoby na podstawie podanych kryteriów. Metoda zwraca dane testowe.
        /// Dostępne dane testowe w plikach XML w folderze PeselTestData: 
        /// 1. Parametry: pesel = 11111111111 - zwraca 1 osobę.
        /// 2. Parametry: nazwisko = NOWAK, imiePierwsze = TOMASZ, imięOjca = KAZIMIERZ i kryterium daty urodzena: dataOd = 19701001 i dataDo = 19731231 - zwraca 13 osób. 
        /// 3. Parametry: nazwisko = NOWAK, imiePierwsze = TOMASZ - zwraca bład biznesowy - Znaleziono więcej niż 50 osób!.
        /// 4. Inne parametry - zwraca 0 osób.
        /// </summary>
        /// <param name="body">The request containing the search criteria for the person. Cannot be null.</param>
        /// <param name="requestId">An optional identifier for tracking the request. Can be null.</param>
        /// <param name="ct">A cancellation token to observe while waiting for the task to complete.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains a  <see
        /// cref="ProxyResponse{T}"/> wrapping a <see cref="SearchPersonResponse"/> with the search results.</returns>
        /// <exception cref="NotImplementedException"></exception>
        async Task<ProxyResponse<SearchPersonResponse>> IPeselService.SearchPersonAsync(SearchPersonRequest body, string? requestId, CancellationToken ct)
        {
            requestId ??= Guid.NewGuid().ToString();

            if (!TryValidateAndNormalize(body, requestId, allowRange: true, out var err))
                return err!;

            var jsonPath = Path.Combine(_testDataDir, "SearchPersonResponse_13.json");

            SearchPersonResponse resp;
            try
            {
                await using var fs = File.OpenRead(jsonPath);
                var proxy = await JsonSerializer.DeserializeAsync<ProxyResponse<SearchPersonResponse>>(fs, JsonOpts, ct);
                resp = proxy?.Data ?? new SearchPersonResponse();
            }
            catch (FileNotFoundException)
            {
                return Error<SearchPersonResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, $"Brak pliku z danymi testowymi: {jsonPath}");
            }
            catch (JsonException jx)
            {
                _logger.LogError(jx, "Błąd JSON przy wczytywaniu {Path}", jsonPath);
                return Error<SearchPersonResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, "Uszkodzony plik danych testowych (JSON).");
            }

            // === Filtrowanie danych testowych wg. żądania ===
            static string N(string? s) => (s ?? "").Trim().ToUpperInvariant();

            if (!string.IsNullOrWhiteSpace(body.Pesel))
                resp.Persons = resp.Persons.Where(p => p.Pesel == body.Pesel).ToList();
            else
                resp.Persons = resp.Persons.Where(p =>
                       N(p.Nazwisko) == N(body.Nazwisko)
                    && N(p.ImiePierwsze) == N(body.ImiePierwsze)
                    && (string.IsNullOrWhiteSpace(body.ImieOjca) || N("KAZIMIERZ") == N(body.ImieOjca))
                ).ToList();

            if (!string.IsNullOrWhiteSpace(body.DataUrodzenia))
                resp.Persons = resp.Persons.Where(p => p.DataUrodzenia == body.DataUrodzenia).ToList();

            // Reguła testowa: „>50 osób” gdy Nowak/Tomasz i nie podano imienia ojca
            if (N(body.Nazwisko) == "NOWAK" && N(body.ImiePierwsze) == "TOMASZ" && string.IsNullOrWhiteSpace(body.ImieOjca))
            {
                return ProxyResponseError<SearchPersonResponse>(requestId, HttpStatusCode.NotAcceptable,
                    ProxyStatus.BusinessError, "Znaleziono więcej niż 50 osób!");
            }

            // Zwróć jak zwykle ProxyResponse<>, zdjęcia już są w JSON (pole "zdjecie")
            return new ProxyResponse<SearchPersonResponse>
            {
                RequestId = requestId,
                Data = resp,
                Source = "SRP",
                Status = ProxyStatus.Success,
                StatusCode = (int)HttpStatusCode.OK
            };
        }

    }
}


