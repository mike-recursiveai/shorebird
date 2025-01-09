using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace QuickTrack.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class AnalyticsController : ControllerBase
    {
        private readonly IAnalyticsService _analyticsService;

        public AnalyticsController(IAnalyticsService analyticsService)
        {
            _analyticsService = analyticsService ?? throw new ArgumentNullException(nameof(analyticsService));
        }

        /// <summary>
        /// Gets patch install counts for the specified filters
        /// </summary>
        /// <param name="accountId">Optional account ID to filter results</param>
        /// <param name="appId">Optional app ID to filter results</param>
        /// <param name="month">Optional month in YYYY-MM format (defaults to previous month)</param>
        /// <returns>Patch install counts for the specified period</returns>
        /// <response code="200">Returns the patch install counts</response>
        /// <response code="400">Invalid month format or future month specified</response>
        /// <response code="401">Missing or invalid authentication</response>
        /// <response code="403">User doesn't have access to specified account/app</response>
        /// <response code="404">Specified account or app not found</response>
        [HttpGet("patch-installs")]
        [ProducesResponseType(typeof(PatchInstallsResponse), 200)]
        [ProducesResponseType(400)]
        [ProducesResponseType(401)]
        [ProducesResponseType(403)]
        [ProducesResponseType(404)]
        public async Task<IActionResult> GetPatchInstalls(
            [FromQuery] string? accountId = null,
            [FromQuery] string? appId = null,
            [FromQuery] string? month = null)
        {
            // Validate month format if provided
            if (month != null && !DateTime.TryParseExact(month, "yyyy-MM", 
                System.Globalization.CultureInfo.InvariantCulture, 
                System.Globalization.DateTimeStyles.None, 
                out DateTime parsedMonth))
            {
                return BadRequest("Invalid month format. Use YYYY-MM format.");
            }

            // Check if month is in the future
            if (parsedMonth > DateTime.UtcNow)
            {
                return BadRequest("Future months are not allowed.");
            }

            try
            {
                var result = await _analyticsService.GetPatchInstallsAsync(
                    accountId,
                    appId,
                    month ?? DateTime.UtcNow.AddMonths(-1).ToString("yyyy-MM"));

                return Ok(result);
            }
            catch (UnauthorizedAccessException)
            {
                return Forbid();
            }
            catch (KeyNotFoundException)
            {
                return NotFound();
            }
            catch (Exception ex)
            {
                // Log the exception here
                return StatusCode(500, "An unexpected error occurred.");
            }
        }
    }

    public class PatchInstallsResponse
    {
        public string Month { get; set; } = string.Empty;
        public int TotalInstalls { get; set; }
        public List<AppInstalls> ByApp { get; set; } = new();
    }

    public class AppInstalls
    {
        public string AppId { get; set; } = string.Empty;
        public string AppName { get; set; } = string.Empty;
        public int InstallCount { get; set; }
    }

    public interface IAnalyticsService
    {
        Task<PatchInstallsResponse> GetPatchInstallsAsync(string? accountId, string? appId, string month);
    }
}