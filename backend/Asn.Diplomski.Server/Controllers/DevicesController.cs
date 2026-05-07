using Asn.Diplomski.Application.UseCases.ConnectDevice;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace Asn.Diplomski.Server.Controllers
{
    /// <summary>
    /// Upravljanje uređajima (mikrokontroleri)
    /// </summary>
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class DevicesController : ControllerBase
    {
        private readonly ConnectDeviceHandler _connectDeviceHandler;
        private readonly ILogger<DevicesController> _logger;

        public DevicesController(
            ConnectDeviceHandler connectDeviceHandler,
            ILogger<DevicesController> logger)
        {
            _connectDeviceHandler = connectDeviceHandler;
            _logger = logger;
        }

        /// <summary>
        /// Registracija uređaja na broker — mikrokontroler poziva ovaj endpoint pri pokretanju.
        /// Server se pretplaćuje na MQTT topic uređaja i počinje primati podatke senzora.
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <response code="200">Pretplata aktivna, server sluša poruke uređaja</response>
        /// <response code="404">Uređaj ne postoji</response>
        [AllowAnonymous]
        [HttpPost("{id:long}/connect")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Connect(long id)
        {
            var result = await _connectDeviceHandler.HandleAsync(id);

            if (result.IsNotFound)
            {
                _logger.LogWarning(
                    "Connect zahtjev odbijen — uređaj s ID-em {DeviceId} ne postoji.", id);
                return NotFound(new { message = $"Uređaj s ID-em '{id}' ne postoji." });
            }

            return Ok(new
            {
                message = "Pretplata aktivna.",
                topic   = result.Topic
            });
        }
    }
}
