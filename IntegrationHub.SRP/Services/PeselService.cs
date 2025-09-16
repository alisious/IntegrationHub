using IntegrationHub.Common.Config;
using IntegrationHub.Common.Contracts;
using IntegrationHub.Common.Helpers;
using IntegrationHub.SRP.Config;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Extensions;
using IntegrationHub.SRP.Models;
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

        
        public async Task<ProxyResponse<GetPersonByPeselResponse>> GetPersonByPeselAsync(GetPersonByPeselRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            

            try
            {

                if (string.IsNullOrWhiteSpace(body.Pesel))
                {
                    throw new ArgumentException("Brak wymaganego parametru: pesel");
                }

                var envelope = RequestEnvelopeHelper.PrepareGetPersonByPeselEnvelope(body, requestId);
                var result = await _soapInvoker.InvokeAsync(_srpConfig.PeselShareServiceUrl, SrpSoapActions.Pesel_UdostepnijAktualneDaneOsobyPoPesel,
                                                        envelope, requestId, ct);
                if (result.Fault is not null)
                {
                    var msg = result.Fault.DetailOpis ?? result.Fault.FaultString;
                    if (!string.IsNullOrWhiteSpace(result.Fault.DetailOpisTechniczny))
                        msg += $"; {result.Fault.DetailOpisTechniczny}";

                    return ProxyResponseError<GetPersonByPeselResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, msg);
                }

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                {
                    return ProxyResponseError<GetPersonByPeselResponse>(requestId, result.StatusCode,
                        ProxyStatus.TechnicalError, $"HTTP {(int)result.StatusCode}");
                }

                // TODO: Zmapuj response XML -> GetPersonResponse (teraz wkładamy RAW w NumerPesel)
                var responseObj = PeselGetPersonByPeselResponseXmlMapper.Parse(result.Body);
                if (responseObj.daneOsoby is null)
                {
                    return ProxyResponseError<GetPersonByPeselResponse>(requestId, HttpStatusCode.NotFound,
                        ProxyStatus.BusinessError, "Brak osoby o podanym PESEL.");
                }

                return new ProxyResponse<GetPersonByPeselResponse>
                {
                    RequestId = requestId,
                    Data = responseObj,
                    Source = "SRP",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (ArgumentException aex)
            {
                return ProxyResponseError<GetPersonByPeselResponse>(requestId, HttpStatusCode.BadRequest,
                    ProxyStatus.BusinessError, aex.Message);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Błąd GetPerson, RequestId: {RequestId}", requestId);
                return ProxyResponseError<GetPersonByPeselResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, ex.Message);
            }
        }

        public async Task<ProxyResponse<SearchPersonResponse>> SearchPersonAsync(SearchPersonRequest body, string? requestId, CancellationToken ct)
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
                    SourceStatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Blad SearchBasePerson, RequestId: {RequestId}", requestId);
                return Error<SearchPersonResponse>(requestId, HttpStatusCode.InternalServerError,
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
                SourceStatusCode = (int)code,
                ErrorMessage = message
            };
        }
    }
}


