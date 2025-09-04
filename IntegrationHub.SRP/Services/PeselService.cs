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
using static IntegrationHub.SRP.Services.SrpServiceCommon;

namespace IntegrationHub.SRP.Services
{
    public sealed class PeselService : IPeselService
    {
        private readonly SrpConfig _srpConfig;
        private readonly ISrpSoapInvoker _soapInvoker;
        private readonly ILogger<PeselService> _logger;
        private readonly IRdoService _rdoService;

        public PeselService(IOptions<SrpConfig> srpConfig, ISrpSoapInvoker soapInvoker, IRdoService rdoService, ILogger<PeselService> logger)
        {
            _srpConfig = srpConfig.Value;
            _soapInvoker = soapInvoker;
            _rdoService = rdoService;
            _logger = logger;
        }

        public async Task<ProxyResponse<SearchPersonResponse>> SearchBasePersonDataAsync(SearchPersonRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            if (!TryValidateAndNormalize(body, requestId, allowRange: false, out var err))
                return err!;

            var envelope = RequestEnvelopeHelper.PrepareSearchPersonBaseDataEnvelope(body, requestId);

            try
            {
                var result = await _soapInvoker.InvokeAsync(
                    _srpConfig.PeselSearchServiceUrl, SrpSoapActions.Pesel_WyszukajOsoby,
                    envelope, requestId, ct);

                if (result.Fault is not null)
                    return Error<SearchPersonResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, result.Fault.FaultString);

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                    return Error<SearchPersonResponse>(requestId, result.StatusCode,
                        ProxyStatus.TechnicalError, $"HTTP {(int)result.StatusCode}");

                var responseObj = new SearchPersonResponse();
                responseObj.Persons.Add(new FoundPerson { Pesel = result.Body });

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
                return Error<SearchPersonResponse>(requestId, HttpStatusCode.InternalServerError,
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

                var envelope = RequestEnvelopeHelper.PrepareGetPersonEnvelope(body, requestId);
                var result = await _soapInvoker.InvokeAsync(_srpConfig.PeselShareServiceUrl, SrpSoapActions.Pesel_UdostepnijAktualneDaneOsobyPoId,
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

        async Task<ProxyResponse<SearchPersonResponse>> IPeselService.SearchPersonAsync(SearchPersonRequest body, string? requestId, CancellationToken ct)
        {
            requestId ??= Guid.NewGuid().ToString();
            if (!TryValidateAndNormalize(body, requestId, allowRange: true, out var err))
                return err!;

            var envelope = RequestEnvelopeHelper.PrepareSearchPersonEnvelope(body, requestId);

            try
            {
                var result = await _soapInvoker.InvokeAsync(
                    _srpConfig.PeselSearchServiceUrl, SrpSoapActions.Pesel_WyszukajOsoby,
                    envelope, requestId, ct);

                if (result.Fault is not null)
                    return Error<SearchPersonResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, result.Fault.FaultString);

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                    return Error<SearchPersonResponse>(requestId, result.StatusCode,
                        ProxyStatus.TechnicalError, $"HTTP {(int)result.StatusCode}");

                var responseObj = PeselSearchPersonResponseXmlMapper.Parse(result.Body);

                // filtr: tylko żyjących
                if (body.CzyZyje == true)
                    responseObj.Persons.RemoveAll(p => p.CzyZyje == false);

                //Pobranie zdjęć
                // Lista żądań do RDO z obu polami
                var photoReqs = responseObj.Persons
                    .Where(p => p.CzyZyje == true
                             && !string.IsNullOrWhiteSpace(p.IdOsoby)
                             && !string.IsNullOrWhiteSpace(p.Pesel))
                    .Select(p => new GetCurrentPhotoRequest
                    {
                        IdOsoby = p.IdOsoby!,   // <-- wymagane
                        Pesel = p.Pesel!      // <-- wymagane
                    })
                    .DistinctBy(r => (r.IdOsoby, r.Pesel))  // uniknij duplikatów
                    .ToList();

                // Pobranie paczki zdjęć z RDO
                if (photoReqs.Count > 0)
                {
                    var photoResults = await RdoBulkHelpers.BulkGetCurrentPhotosAsync(
                        _rdoService, photoReqs, maxParallel: 6, ct);

                    //Mapuj wyniki po kluczu (IdOsoby, Pesel) do osób
                    var byKey = photoResults.ToDictionary(
                        x => (x.Request.IdOsoby, x.Request.Pesel), x => x.Result);

                    foreach (var person in responseObj.Persons)
                    {
                        if (!string.IsNullOrWhiteSpace(person.IdOsoby) && !string.IsNullOrWhiteSpace(person.Pesel) &&
                            byKey.TryGetValue((person.IdOsoby, person.Pesel), out var pr) &&
                            pr.Status == ProxyStatus.Success && pr.Data != null)
                        {
                            // Pobierz pierwsze zdjęcie z GetCurrentPhotoResponse)
                            person.Zdjecie = pr.Data.GetFirstPhotoOrDefault();
                        }
                    }
                }

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
                _logger.LogError(ex, "Blad SearchBasePerson, RequestId: {RequestId}", requestId);
                return Error<SearchPersonResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, ex.Message);
            }

        }
    }
}


