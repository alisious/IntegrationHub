using IntegrationHub.Common.Contracts;                 // + ProxyResponse, ProxyStatus
using IntegrationHub.Sources.CEP.Contracts;
using IntegrationHub.Sources.CEP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Swashbuckle.AspNetCore.Annotations;              // (opcjonalnie) ładniejszy opis w Swagger
using System.Net;

namespace IntegrationHub.Api.Controllers;

[ApiController]
[Route("CEP/slowniki")]
public sealed class CEPSlownikiController : ControllerBase
{
    private readonly ICEPSlownikiService _service;
    private readonly ILogger<CEPSlownikiController> _logger;

    public CEPSlownikiController(ICEPSlownikiService service, ILogger<CEPSlownikiController> logger)
    {
        _service = service;
        _logger = logger;
    }

    /// <summary>Lista nagłówków słowników (CEP – pobierzListeSlownikow).</summary>
    [SwaggerOperation(Summary = "Pobiera listę słowników z CEP.")]
    [HttpGet("lista")]
    [ProducesResponseType(typeof(ProxyResponse<List<SlownikNaglowekDto>>), StatusCodes.Status200OK)]
    public async Task<ProxyResponse<List<SlownikNaglowekDto>>> PobierzListeSlownikow(CancellationToken ct)
    {
        var requestId = Guid.NewGuid().ToString();

        try
        {
            var data = await _service.PobierzListeSlownikowAsync(ct)
                       ?? new List<SlownikNaglowekDto>();

            return new ProxyResponse<List<SlownikNaglowekDto>>
            {
                RequestId = requestId,
                Source = "CEP",
                Status = ProxyStatus.Success,
                SourceStatusCode = (int)HttpStatusCode.OK,
                Data = data
            };
        }
        catch (OperationCanceledException)
        {
            // przekazujemy dalej — nie „opakowujemy” anulowania
            throw;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Błąd w CEP/pobierzListeSlownikow. RequestId={RequestId}", requestId);

            return new ProxyResponse<List<SlownikNaglowekDto>>
            {
                RequestId = requestId,
                Source = "CEP",
                Status = ProxyStatus.TechnicalError,          // jak w SRP przy błędach technicznych
                SourceStatusCode = (int)HttpStatusCode.InternalServerError,
                ErrorMessage = ex.Message
            };
        }
    }
}
