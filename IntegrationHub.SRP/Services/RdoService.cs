using IntegrationHub.Common.Configs;
using IntegrationHub.Common.Contracts;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Helpers;
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
            var hasPersonId = !string.IsNullOrWhiteSpace(body.PersonId);
            if (!(hasPesel && hasPersonId))
            {
                return ProxyResponseError<GetCurrentPhotoResponse>(requestId, HttpStatusCode.BadRequest,
                    ProxyStatus.BusinessError, "Brak numeru PESEL i ID osoby do wyszukania zdjęcia.");
            }

            var envelope = SoapHelper.PrepareGetCurrentPhotoRequestEnvelope(body, requestId);

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

                    return ProxyResponseError<GetCurrentPhotoResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, msg);
                }

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                {
                    return ProxyResponseError<GetCurrentPhotoResponse>(requestId, result.StatusCode,
                        ProxyStatus.TechnicalError, $"HTTP {(int)result.StatusCode}");
                }

                var parsed = SrpResponseParser.ParseGetCurrentPhotoResponse(
                    result.Body, _logger, requestId, validateBase64: true, snippetChars: 16);

                return new ProxyResponse<GetCurrentPhotoResponse>
                {
                    RequestId = requestId,
                    Data = parsed,
                    Source = "SRP",
                    Status = ProxyStatus.Success,
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (TimeoutException)
            {
                return ProxyResponseError<GetCurrentPhotoResponse>(requestId, HttpStatusCode.RequestTimeout,
                    ProxyStatus.TechnicalError, "Przekroczono czas oczekiwania na odpowiedz uslugi SRP: udostepnijAktualneZdjecie.");
            }
            catch (CommunicationException cex)
            {
                return ProxyResponseError<GetCurrentPhotoResponse>(requestId, HttpStatusCode.BadGateway,
                    ProxyStatus.TechnicalError, $"Blad komunikacji z usluga SRP: udostepnijAktualneZdjecie. {cex.Message}");
            }
            catch (Exception ex)
            {
                return ProxyResponseError<GetCurrentPhotoResponse>(requestId, HttpStatusCode.InternalServerError,
                    ProxyStatus.TechnicalError, ex.Message);
            }
        }

        public async Task<ProxyResponse<GetIdCardResponse>> ShareIdCardDataAsync(GetIdCardRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();

            var hasPeselList = body.NumeryPesel is { Count: > 0 };
            if (!hasPeselList)
            {
                return ProxyResponseError<GetIdCardResponse>(requestId, HttpStatusCode.BadRequest,
                    ProxyStatus.BusinessError, "Brak numerów PESEL do wyszukania dowodów osobistych.");
            }

            var envelope = SoapHelper.PrepareShareIdCardRequestEnvelope(body);

            try
            {
                var result = await _invoker.InvokeAsync(_srp.RdoShareServiceUrl, SrpSoapActions.Rdo_UdostepnijDaneAktualnychDowodowPoPesel,
                                                        envelope, requestId, ct);

                if (result.Fault is not null)
                {
                    var msg = result.Fault.DetailOpis ?? result.Fault.FaultString;
                    if (!string.IsNullOrWhiteSpace(result.Fault.DetailOpisTechniczny))
                        msg += $"; {result.Fault.DetailOpisTechniczny}";

                    return ProxyResponseError<GetIdCardResponse>(requestId, HttpStatusCode.BadRequest,
                        ProxyStatus.BusinessError, msg);
                }

                if ((int)result.StatusCode < 200 || (int)result.StatusCode >= 300)
                {
                    return ProxyResponseError<GetIdCardResponse>(requestId, result.StatusCode,
                        ProxyStatus.TechnicalError, $"HTTP {(int)result.StatusCode}");
                }

                var data = new GetIdCardResponse { IdCardXml = result.Body };
                return new ProxyResponse<GetIdCardResponse>
                {
                    RequestId = requestId,
                    Data = data,
                    Source = "SRP",
                    Status = ProxyStatus.Success,
                    StatusCode = (int)HttpStatusCode.OK
                };
            }
            catch (TimeoutException)
            {
                return ProxyResponseError<GetIdCardResponse>(requestId, HttpStatusCode.RequestTimeout,
                    ProxyStatus.TechnicalError, "Przekroczono czas oczekiwania na odpowiedz uslugi SRP: udostepnijDaneAktualnychDowodowPoPesel.");
            }
            catch (CommunicationException cex)
            {
                return ProxyResponseError<GetIdCardResponse>(requestId, HttpStatusCode.BadGateway,
                    ProxyStatus.TechnicalError, $"Blad komunikacji z usluga SRP: udostepnijDaneAktualnychDowodowPoPesel. {cex.Message}");
            }
            catch (Exception ex)
            {
                return ProxyResponseError<GetIdCardResponse>(requestId, HttpStatusCode.InternalServerError,
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
