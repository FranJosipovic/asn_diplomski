using Asn.Diplomski.Application.UseCases.CompleteDeviceProvisioning;
using Asn.Diplomski.Application.UseCases.GetProvisioningToken;
using Asn.Diplomski.Server.DTOs;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace Asn.Diplomski.Server.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class ProvisionController : ControllerBase
    {
        private readonly GetProvisioningTokenHandler _getProvisioningTokenHandler;
        private readonly CompleteDeviceProvisioningHandler _completeDeviceProvisioningHandler;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProvisionController> _logger;

        public ProvisionController(
            GetProvisioningTokenHandler getProvisioningTokenHandler,
            CompleteDeviceProvisioningHandler completeDeviceProvisioningHandler,
            IConfiguration configuration,
            ILogger<ProvisionController> logger)
        {
            _getProvisioningTokenHandler = getProvisioningTokenHandler;
            _completeDeviceProvisioningHandler = completeDeviceProvisioningHandler;
            _configuration = configuration;
            _logger = logger;
        }

        [HttpGet]
        [ProducesResponseType(typeof(ProvisioningTokenResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetProvisioningTokens()
        {
            var tenantIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (tenantIdClaim == null || !long.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                _logger.LogWarning("Provisioning token zahtjev odbijen — ne mogu ekstrahirati tenant ID iz JWT");
                return Unauthorized(new { message = "Ne mogu ekstrahirati tenant ID iz JWT tokena." });
            }

            var command = new GetProvisioningTokenCommand(tenantId);
            var result = await _getProvisioningTokenHandler.HandleAsync(command);

            if (result.IsNotFound)
            {
                _logger.LogWarning("Provisioning token zahtjev odbijen — tenant s ID-em {TenantId} ne postoji", tenantId);
                return NotFound(new { message = $"Tenant s ID-em '{tenantId}' ne postoji." });
            }

            return Ok(new ProvisioningTokenResponseDto
            {
                ExpiresAt = result.ExpiresAt,
                ServerHost = HttpContext.Request.Host.Host,
                ServerPort = HttpContext.Request.Host.Port ?? (HttpContext.Request.IsHttps ? 443 : 80),
                Devices = result.Devices.Select(d => new DeviceProvisioningDto
                {
                    DeviceId = d.DeviceId,
                    DeviceType = d.DeviceType.ToString(),
                    DeviceSsid = d.DeviceSsid,
                    ProvisionStatus = d.ProvisionStatus.ToString(),
                    ProvisioningToken = d.ProvisioningToken,
                    Sensors = d.Sensors.Select(s => new SensorProvisioningDto
                    {
                        SensorId = s.SensorId,
                        SensorType = s.SensorType.ToString()
                    }).ToList()
                }).ToList()
            });
        }

        [HttpPost("{provisioningToken}")]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CompleteProvisioningResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        public async Task<IActionResult> CompleteProvisioning(string provisioningToken)
        {
            var command = new CompleteDeviceProvisioningCommand(provisioningToken);
            var result = await _completeDeviceProvisioningHandler.HandleAsync(command);

            if (result.IsNotFound)
            {
                _logger.LogWarning("Provisioning odbijen — token nije pronađen");
                return NotFound(new { message = "Provisioning token nije pronađen." });
            }

            if (result.IsExpired)
            {
                _logger.LogWarning("Provisioning odbijen — token istekao za uređaj {DeviceId}", result.DeviceId);
                return StatusCode(StatusCodes.Status410Gone, new { message = "Provisioning token je istekao." });
            }

            var mqttHost = _configuration["Mqtt:Host"] ?? "localhost";
            var mqttPort = int.Parse(_configuration["Mqtt:Port"] ?? "1883");

            return Ok(new CompleteProvisioningResponseDto
            {
                TenantId = result.TenantId,
                DeviceId = result.DeviceId,
                MqttHost = mqttHost,
                MqttPort = mqttPort,
                Sensors = result.Sensors.Select(s => new SensorProvisioningDto
                {
                    SensorId = s.SensorId,
                    SensorType = s.SensorType.ToString()
                }).ToList()
            });
        }
    }
}
