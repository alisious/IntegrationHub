using IntegrationHub.Common.Config;
using IntegrationHub.Common.Contracts;
using IntegrationHub.SRP.Config;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Extensions;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;


namespace IntegrationHub.SRP.Services
{
    public sealed class RdoService : IRdoService
    {
        private readonly SrpConfig _srp;
        private readonly ISrpSoapInvoker _invoker;
        private readonly ILogger<RdoService> _logger;
       

        public RdoService(IOptions<SrpConfig> srpConfig, ISrpSoapInvoker invoker, ILogger<RdoService> logger)
        {
            _srp = srpConfig.Value;
            _invoker = invoker;
            _logger = logger;
        }

        public async Task<ProxyResponse<GetCurrentPhotoResponse>> GetCurrentPhotoAsync(GetCurrentPhotoRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            var hasPesel = !string.IsNullOrWhiteSpace(body.Pesel);
            var hasPersonId = !string.IsNullOrWhiteSpace(body.IdOsoby);
            if (!(hasPesel && hasPersonId))
            {
                return Error<GetCurrentPhotoResponse>(requestId, HttpStatusCode.BadRequest,
                    ProxyStatus.BusinessError, "Brak numeru PESEL i ID osoby do wyszukania zdjęcia.");
            }

            var envelope = RequestEnvelopeHelper.PrepareGetCurrentPhotoRequestEnvelope(body, requestId);

            try
            {
                var result = await _invoker.InvokeAsync(_srp.RdoShareServiceUrl, SrpSoapActions.Rdo_UdostepnijAktualneZdjecie,
                                                        envelope, requestId, ct);

                if (result.Fault is not null)
                {
                    // Preferuj opis biznesowy, a techniczny dorzuć po średniku (o ile jest)
                    var msg = result.Fault.DetailOpis ?? result.Fault.FaultString;
                    if (!string.IsNullOrWhiteSpace(result.Fault.DetailOpisTechniczny))
                        msg += $"; {result.Fault.DetailOpisTechniczny}";

                    return Error<GetCurrentPhotoResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, msg);
                }

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                {
                    return Error<GetCurrentPhotoResponse>(requestId, result.StatusCode,
                        ProxyStatus.TechnicalError, $"HTTP {(int)result.StatusCode}");
                }

                var parsed = RdoGetCurrentPhotoResponseXmlMapper.Parse(
                    result.Body, _logger, requestId, validateBase64: true, snippetChars: 16);

                return new ProxyResponse<GetCurrentPhotoResponse>
                {
                    RequestId = requestId,
                    Data = parsed,
                    Source = "SRP",
                    Status = ProxyStatus.Success,
                    SourceStatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (TimeoutException)
            {
                return Error<GetCurrentPhotoResponse>(requestId, HttpStatusCode.RequestTimeout,
                    ProxyStatus.TechnicalError, "Przekroczono czas oczekiwania na odpowiedz uslugi SRP: udostepnijAktualneZdjecie.");
            }
            catch (CommunicationException cex)
            {
                return Error<GetCurrentPhotoResponse>(requestId, HttpStatusCode.BadGateway,
                    ProxyStatus.TechnicalError, $"Blad komunikacji z usluga SRP: udostepnijAktualneZdjecie. {cex.Message}");
            }
            catch (Exception ex)
            {
                return Error<GetCurrentPhotoResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, ex.Message);
            }
        }


        /// <summary>
        /// Udostępnia aktualne dane z dowodu osobistego na podstawie numeru PESEL.
        /// </summary>
        /// <param name="body"></param>
        /// <param name="requestId"></param>
        /// <param name="ct"></param>
        /// <returns></returns>
        /// <exception cref="NotImplementedException"></exception>
        public async Task<ProxyResponse<GetCurrentIdByPeselResponse>> GetCurrentIdByPeselAsync(
            GetCurrentIdByPeselRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            if (string.IsNullOrWhiteSpace(body.Pesel))
            {
                return Error<GetCurrentIdByPeselResponse>(requestId, HttpStatusCode.BadRequest, ProxyStatus.BusinessError, "PESEL jest wymagany.");
            }

            // Budowa koperty SOAP 1.1
            var envelope = RequestEnvelopeHelper.PrepareGetCurrentIdByPeselRequestEnvelope(body, requestId);

            try
            {
                var result = await _invoker.InvokeAsync(
                    _srp.RdoShareServiceUrl, 
                    SrpSoapActions.Rdo_UdostepnijDaneAktualnegoDowoduPoPesel,
                    envelope,
                    requestId,
                    ct);

                if (result.Fault is not null)
                    return Error<GetCurrentIdByPeselResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, result.Fault.FaultString);

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                    return Error<GetCurrentIdByPeselResponse>(requestId, result.StatusCode,
                        ProxyStatus.TechnicalError, $"HTTP {(int)result.StatusCode}");


                // Parsowanie
                var responseObj =  RdoGetCurrentIdResponseXmlMapper.Parse(result.Body);

                
                if (responseObj.Dowod is null)
                {
                    _logger.LogWarning("SRP:RDO Brak danych dowodu osobistego w odpowiedzi SRP (PESEL={Pesel}) RID={RID}", body.Pesel, requestId);
                    return Error<GetCurrentIdByPeselResponse>(requestId, HttpStatusCode.NotFound,
                        ProxyStatus.BusinessError, "Brak danych dowodu osobistego dla podanego numeru PESEL.");
                }

                return new ProxyResponse<GetCurrentIdByPeselResponse>
                {
                    RequestId = requestId,
                    Data = responseObj,
                    Status = ProxyStatus.Success,
                    SourceStatusCode = (int)HttpStatusCode.OK,
                    Source = "SRP"
                };
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "SRP:RDO Błąd UdostepnijDaneAktualnegoDowoduPoPesel (PESEL={Pesel}) RID={RID}", body.Pesel, requestId);
                return Error<GetCurrentIdByPeselResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, ex.Message);
            }
        }

        private static ProxyResponse<T> Error<T>(string requestId, HttpStatusCode code, ProxyStatus status, string message)
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
