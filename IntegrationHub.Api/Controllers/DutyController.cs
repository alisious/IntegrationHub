using IntegrationHub.PIESP.Exceptions;
using IntegrationHub.PIESP.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;

namespace IntegrationHub.PIESP.Controllers
{
    // DutyController.cs
    /// <summary>
    /// Kontroler odpowiedzialny za operacje związane ze służbami użytkownika.
    /// </summary>
    [Authorize(Roles = "User")]
    [ApiController]
    [Route("piesp/[controller]")]
    //[ApiExplorerSettings(GroupName = "PIESP")]
    public class DutyController : ControllerBase
    {
        private readonly DutyService _duties;

        public DutyController(DutyService duties)
        {
            _duties = duties;
        }

        /// <summary>
        /// Pobiera listę służb użytkownika na podany dzień.
        /// </summary>
        /// <param name="date">Data (domyślnie dzisiejsza).</param>
        /// <returns>Lista służb.</returns>
        [HttpGet("my-duties")]
        public IActionResult GetMyDuties([FromQuery] DateTime? date = null)
        {
            var badge = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var duties = _duties.GetDutiesForUser(badge, date ?? DateTime.Today);
            return Ok(duties);
        }

        /// <summary>
        /// Pobiera bieżącą służbę użytkownika.
        /// </summary>
        /// <returns>Szczegóły bieżącej służby lub 404 jeśli brak.</returns>
        [HttpGet("my-current-duty")]
        public IActionResult GetMyCurrentDuty()
        {
            var badge = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var duty = _duties.GetCurrentDutyForUser(badge);
            return duty != null ? Ok(duty) : NotFound($"Użytkownik z odznaką {badge} nie pełni teraz służby.");
        }



        /// <summary>
        /// Rozpoczyna służbę o podanym ID.
        /// </summary>
        /// <param name="dutyId">ID służby.</param>
        /// <param name="req">Data i czas rozpoczęcia.</param>
        /// <returns>200 OK przy sukcesie, 403 jeśli nieautoryzowany, 409 jeśli użytkownik ma inną służbę.</returns>
        [HttpPost("{dutyId}/start")]
        public IActionResult StartDuty(int dutyId, [FromBody] StartEndDutyRequest req)
        {
            // Check if user is authorized to start this duty
            // User are allowed to start only their own duties

            var badge = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userDuty = _duties.GetDutyForUser(dutyId,badge);
            if (userDuty is null)
                return Forbid("You are not authorized to start this duty.");

            try
            {
                var result = _duties.StartDuty(dutyId, req.DateTime);
                return result ? Ok() : BadRequest("Cannot start duty.");
            }
            catch (UserAlreadyOnDutyException ex)
            {
                return Conflict(new { message = ex.Message });
            }
        }

        /// <summary>
        /// Kończy służbę o podanym ID.
        /// </summary>
        /// <param name="dutyId">ID służby.</param>
        /// <param name="req">Data i czas zakończenia.</param>
        /// <returns>200 OK lub 403/400.</returns>
        [HttpPost("{dutyId}/finish")]
        public IActionResult FinishDuty(int dutyId, [FromBody] StartEndDutyRequest req)
        {
            var badge = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var userDuties = _duties.GetDutiesForUser(badge, req.DateTime);
            if (!userDuties.Any(d => d.Id == dutyId))
                return Forbid("You are not authorized to finish this duty.");

            var result = _duties.FinishDuty(dutyId, req.DateTime);
            return result ? Ok() : BadRequest("Cannot finish duty.");
        }

        /// <summary>
        /// Model żądania rozpoczęcia lub zakończenia służby.
        /// </summary>
        /// <param name="DateTime">Data i godzina operacji.</param>
        public record StartEndDutyRequest(DateTime DateTime);
    }

}
