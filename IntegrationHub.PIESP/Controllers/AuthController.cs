using IntegrationHub.PIESP.Services;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace IntegrationHub.PIESP.Controllers
{
    // AuthController.cs
    /// <summary>
    /// Kontroler odpowiedzialny za logowanie i reset PIN-u użytkowników.
    /// </summary>
    [ApiController]
    [Route("piesp/[controller]")]
    [ApiExplorerSettings(GroupName = "PIESP")]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly SupervisorService _supervisorService;
        private readonly IConfiguration _config;

        public AuthController(AuthService authService, SupervisorService supervisorService, IConfiguration config)
        {
            _authService = authService;
            _supervisorService = supervisorService;
            _config = config;
        }

        /// <summary>
        /// Resetuje PIN użytkownika po weryfikacji kodu bezpieczeństwa.
        /// </summary>
        /// <param name="req">Numer odznaki, kod bezpieczeństwa i nowy PIN.</param>
        /// <returns>Kod 200 przy sukcesie, 400 przy błędnym kodzie, 404 jeśli użytkownik nie istnieje.</returns>
        [HttpPost("reset-pin")]
        public async Task<IActionResult> ResetPin([FromBody] ResetPinRequest req)
        {
            var valid = await _supervisorService.ValidateSecurityCodeAsync(req.BadgeNumber, req.SecurityCode);
            if (!valid) return BadRequest("Invalid security code.");
            var user = await _authService.SetPinAsync(req.BadgeNumber, req.NewPin);
            return user != null ? Ok() : NotFound();
        }

        /// <summary>
        /// Loguje użytkownika i zwraca token JWT.
        /// </summary>
        /// <param name="req">Numer odznaki i PIN.</param>
        /// <returns>Token JWT, role użytkownika oraz imię i nazwisko.</returns>
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest req)
        {
            var user = await _authService.LoginAsync(req.BadgeNumber, req.Pin);
            if (user == null) return Unauthorized();

            var jwtKey = _config["Jwt:Key"];
            var token = _authService.GenerateJwtToken(user, jwtKey);

            var roles = user.Roles.Select(r => r.Role.ToString());
            return Ok(new { Token = token, Roles = roles, user.UserName });
        }

        /// <summary>
        /// Model żądania resetu PIN-u.
        /// </summary>
        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        /// <param name="SecurityCode">Kod bezpieczeństwa otrzymany od przełożonego.</param>
        /// <param name="NewPin">Nowy PIN.</param>
        public record ResetPinRequest(string BadgeNumber, string SecurityCode, string NewPin);

        /// <summary>
        /// Model żądania logowania.
        /// </summary>
        /// <param name="BadgeNumber">Numer odznaki użytkownika.</param>
        /// <param name="Pin">PIN użytkownika.</param>
        public record LoginRequest(string BadgeNumber, string Pin);

    }
}
