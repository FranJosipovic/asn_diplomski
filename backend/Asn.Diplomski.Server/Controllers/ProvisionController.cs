using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Application.UseCases.CompleteDeviceProvisioning;
using Asn.Diplomski.Application.UseCases.ConfirmMqttConnection;
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
        private readonly ConfirmMqttConnectionHandler _confirmMqttConnectionHandler;
        private readonly IDeviceRepository _deviceRepository;
        private readonly IMqttPublisher _mqttPublisher;
        private readonly IConfiguration _configuration;
        private readonly ILogger<ProvisionController> _logger;

        public ProvisionController(
            GetProvisioningTokenHandler getProvisioningTokenHandler,
            CompleteDeviceProvisioningHandler completeDeviceProvisioningHandler,
            ConfirmMqttConnectionHandler confirmMqttConnectionHandler,
            IDeviceRepository deviceRepository,
            IMqttPublisher mqttPublisher,
            IConfiguration configuration,
            ILogger<ProvisionController> logger)
        {
            _getProvisioningTokenHandler = getProvisioningTokenHandler;
            _completeDeviceProvisioningHandler = completeDeviceProvisioningHandler;
            _confirmMqttConnectionHandler = confirmMqttConnectionHandler;
            _deviceRepository = deviceRepository;
            _mqttPublisher = mqttPublisher;
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

        [HttpPost]
        [AllowAnonymous]
        [ProducesResponseType(typeof(CompleteProvisioningResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status410Gone)]
        public async Task<IActionResult> CompleteProvisioning([FromBody] CompleteProvisioningRequestDto request)
        {
            var command = new CompleteDeviceProvisioningCommand(request.Token);
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
            if (mqttHost is "localhost" or "127.0.0.1")
                mqttHost = HttpContext.Request.Host.Host;
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

        [HttpPost("confirm")]
        [AllowAnonymous]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        public async Task<IActionResult> ConfirmMqttConnection([FromBody] ConfirmMqttConnectionRequestDto request)
        {
            var command = new ConfirmMqttConnectionCommand(request.TenantId, request.DeviceId);
            var result = await _confirmMqttConnectionHandler.HandleAsync(command);

            if (result.IsNotFound)
            {
                _logger.LogWarning("MQTT potvrda odbijena — uređaj {DeviceId} nije pronađen za tenant {TenantId}",
                    request.DeviceId, request.TenantId);
                return NotFound(new { message = "Uređaj nije pronađen." });
            }

            if (result.IsInvalidState)
            {
                _logger.LogWarning("MQTT potvrda odbijena — uređaj {DeviceId} nije u stanju Provisioning",
                    request.DeviceId);
                return Conflict(new { message = "Uređaj nije u stanju provisioning." });
            }

            _logger.LogInformation("Uređaj {DeviceId} (tenant {TenantId}) potvrdio MQTT konekciju",
                request.DeviceId, request.TenantId);
            return NoContent();
        }

        [HttpPost("start")]
        [ProducesResponseType(typeof(StartSystemResponseDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> StartSystem()
        {
            var tenantIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (tenantIdClaim == null || !long.TryParse(tenantIdClaim.Value, out var tenantId))
            {
                _logger.LogWarning("Start zahtjev odbijen — ne mogu ekstrahirati tenant ID iz JWT");
                return Unauthorized(new { message = "Ne mogu ekstrahirati tenant ID iz JWT tokena." });
            }

            var devices = await _deviceRepository.GetAllProvisionedByTenantAsync(tenantId);

            foreach (var device in devices)
                _mqttPublisher.Enqueue(tenantId, device.Id, new { command = "start" });

            _logger.LogInformation("Tenant {TenantId} pokrenuo sustav — poslana 'start' naredba na {Count} uređaja",
                tenantId, devices.Count);

            return Ok(new StartSystemResponseDto
            {
                NotifiedDeviceCount = devices.Count,
                NotifiedDeviceIds = devices.Select(d => d.Id).ToList()
            });
        }
    }
}
