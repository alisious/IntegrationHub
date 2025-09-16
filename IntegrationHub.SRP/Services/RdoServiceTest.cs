using IntegrationHub.Common.Config;
using IntegrationHub.Common.Contracts;
using IntegrationHub.SRP.Config;
using IntegrationHub.SRP.Contracts;
using IntegrationHub.SRP.Extensions;
using IntegrationHub.SRP.Models;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.ServiceModel;
using System.Text;
using System.Threading.Tasks;


namespace IntegrationHub.SRP.Services
{
    public sealed class RdoServiceTest : IRdoService
    {
        private readonly SrpConfig _srp;
        private readonly ISrpSoapInvoker _invoker;
        private readonly ILogger<RdoService> _logger;
        private readonly string _testDataDir;

        public RdoServiceTest(IOptions<SrpConfig> srpConfig, ISrpSoapInvoker invoker, ILogger<RdoService> logger, IHostEnvironment env)
        {
            _srp = srpConfig.Value;
            _invoker = invoker;
            _logger = logger;
            _testDataDir = System.IO.Path.Combine(env.ContentRootPath, "TestData", "SRP");// <contentRoot>\TestData\SRP
        }

        public async Task<ProxyResponse<GetCurrentPhotoResponse>> GetCurrentPhotoAsync(GetCurrentPhotoRequest body, string? requestId = null, CancellationToken ct = default)
        {
            requestId ??= Guid.NewGuid().ToString();
            throw new NotImplementedException();
        }


        /// <summary>
        /// Udostępnia aktualne dane z dowodu osobistego na podstawie numeru PESEL. Numer PESEL jest wymagany. Danne są dostępne dla PESEL = 11111111111.
        /// </summary>
        /// <param name="pesel"></param>
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

            var xmlPath = Path.Combine(_testDataDir, "UdostepnijDaneAktualnegoDowoduPoPesel_RESPONSE.xml");
            // Budowa koperty SOAP 1.1
            //var envelope = RequestEnvelopeHelper.PrepareGetCurrentIdRequestEnvelope(pesel, requestId);

            try
            {
                var responseObj = new GetCurrentIdByPeselResponse();

                if (body.Pesel != "11111111111")
                {
                    responseObj.Dowod = null;
                    if (responseObj.Dowod is null)
                    {
                        _logger.LogWarning("SRP:RDO Brak danych dowodu osobistego w odpowiedzi SRP (PESEL={Pesel}) RID={RID}", body.Pesel, requestId);
                        return Error<GetCurrentIdByPeselResponse>(requestId, HttpStatusCode.NotFound,
                            ProxyStatus.BusinessError, "Brak danych dowodu osobistego dla podanego numeru PESEL.");
                    }

                }
                
                // Wczytaj XML i sparsuj do DTO (parsuje też zdjęcia i podpis, jeśli są w pliku)
                var xml = await File.ReadAllTextAsync(xmlPath, ct).ConfigureAwait(false);

                // Parsowanie
                //dowodOsobisty = SrpResponseParser.ParseIdCardResponse(xml);
                responseObj = RdoGetCurrentIdResponseXmlMapper.Parse(xml);
                
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
