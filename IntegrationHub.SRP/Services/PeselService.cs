using IntegrationHub.Common.Configs;
using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.Helpers;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Helpers;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.SRP.Services
{
    public sealed class PeselService : IPeselService
    {
        private readonly SrpConfig _srp;
        private readonly ISrpSoapInvoker _invoker;
        private readonly ILogger<PeselService> _logger;

        public PeselService(IOptions<SrpConfig> srpConfig, ISrpSoapInvoker invoker, ILogger<PeselService> logger)
        {
            _srp = srpConfig.Value;
            _invoker = invoker;
            _logger = logger;
        }

        public async Task<ProxyResponse<SearchPersonResponse>> SearchBasePersonDataAsync(SearchPersonRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            var hasPesel = !string.IsNullOrWhiteSpace(body.Pesel);
            var hasNamePair = !string.IsNullOrWhiteSpace(body.Nazwisko) && !string.IsNullOrWhiteSpace(body.ImiePierwsze);
            if (!hasPesel && !hasNamePair)
            {
                return ProxyResponseError<SearchPersonResponse>(requestId, HttpStatusCode.BadRequest,
                    ProxyStatus.BusinessError, "Obowiazkowo podaj PESEL albo zestaw: nazwisko i imie.");
            }

            if (!string.IsNullOrWhiteSpace(body.DataUrodzenia))
            {
                var formatted = DateStringFormatHelper.FormatYyyyMmDd(body.DataUrodzenia);
                if (formatted is null)
                {
                    return ProxyResponseError<SearchPersonResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, "Niepoprawny format parametru dataUrodzenia. Wymagany format: yyyyMMdd lub yyyy-MM-dd.");
                }
                body.DataUrodzenia = formatted;
            }

            var envelope = SoapHelper.PrepareSearchPersonBaseDataEnvelope(body, requestId);

            try
            {
                var result = await _invoker.InvokeAsync(_srp.PeselSearchServiceUrl, SrpSoapActions.Pesel_WyszukajOsoby,
                                                        envelope, requestId, ct);

                if (result.Fault is not null)
                {
                    var msg = result.Fault.DetailOpis ?? result.Fault.FaultString;
                    if (!string.IsNullOrWhiteSpace(result.Fault.DetailOpisTechniczny))
                        msg += $"; {result.Fault.DetailOpisTechniczny}";

                    return ProxyResponseError<SearchPersonResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, msg);
                }

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                {
                    return ProxyResponseError<SearchPersonResponse>(requestId, result.StatusCode,
                        ProxyStatus.TechnicalError, $"HTTP {(int)result.StatusCode}");
                }

                // TODO: Zmapuj response XML -> BasicPersonSearchResponse (teraz wkładamy RAW jako placeholder)
                var responseObj = new SearchPersonResponse();
                responseObj.Persons.Add(new SRP.Models.PersonData { NumerPesel = result.Body });

                return new ProxyResponse<SearchPersonResponse>
                {
                    RequestId = requestId,
                    Data = responseObj,
                    Source = "SRP",
                    Status = ProxyStatus.Success,
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blad SearchBasePersonData, RequestId: {RequestId}", requestId);
                return ProxyResponseError<SearchPersonResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, ex.Message);
            }
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

                var envelope = SoapHelper.PrepareGetPersonEnvelope(body, requestId);
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
    }
}


