using IntegrationHub.PIESP.Services;
using IntegrationHub.PIESP.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace IntegrationHub.PIESP.Controllers
{
    /// <summary>
    /// Kontroler operacji przełożonego: generowanie kodów i zarządzanie rolami.
    /// </summary>
    [ApiController]
    [Route("piesp/[controller]")]
    [Authorize(Roles = "Supervisor")]
    //[ApiExplorerSettings(GroupName = "PIESP")]
    public class SupervisorController : ControllerBase
    {
        private readonly SupervisorService _supervisorService;

        public SupervisorController(SupervisorService supervisorService)
        {
            _supervisorService = supervisorService;
        }

        /// <summary>
        /// Generuje kod bezpieczeństwa dla wskazanego numeru odznaki.
        /// </summary>
        /// <param name="req">Numer odznaki.</param>
        /// <returns>Kod bezpieczeństwa.</returns>
        [HttpPost("generate-code")]
        public async Task<IActionResult> GenerateCode([FromBody] GenerateCodeRequest req)
        {
            var code = await _supervisorService.GenerateCodeAsync(req.BadgeNumber);
            return Ok(new { SecurityCode = code });
        }

        /// <summary>
        /// Przypisuje wskazaną rolę użytkownikowi.
        /// </summary>
        /// <param name="req">Numer odznaki i rola.</param>
        /// <returns>200 OK lub 404 jeśli użytkownik nie istnieje.</returns>
        [HttpPost("assign-role")]
        public async Task<IActionResult> AssignRole([FromBody] RoleChangeRequest req)
        {
            var success = await _supervisorService.AssignRoleAsync(req.BadgeNumber, req.Role);
            return success ? Ok() : NotFound("User not found or role already assigned.");
        }

        /// <summary>
        /// Odbiera wskazaną rolę użytkownikowi.
        /// </summary>
        /// <param name="req">Numer odznaki i rola.</param>
        /// <returns>200 OK lub 404 jeśli użytkownik nie istnieje.</returns>
        [HttpPost("revoke-role")]
        public async Task<IActionResult> RevokeRole([FromBody] RoleChangeRequest req)
        {
            var success = await _supervisorService.RevokeRoleAsync(req.BadgeNumber, req.Role);
            return success ? Ok() : NotFound("User or role not found.");
        }

        /// <summary>
        /// Model żądania wygenerowania kodu bezpieczeństwa.
        /// </summary>
        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        public record GenerateCodeRequest(string BadgeNumber);

        /// <summary>
        /// Model żądania zmiany roli użytkownika.
        /// </summary>
        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        /// <param name="Role">Rola do przypisania lub odebrania.</param>
        public record RoleChangeRequest(string BadgeNumber, RoleType Role);
    }
}

