using Asn.Diplomski.Application.UseCases.ConnectDevice;
using Asn.Diplomski.Application.UseCases.ConnectTenant;
using Asn.Diplomski.Application.UseCases.CreateTenant;
using Asn.Diplomski.Application.UseCases.GetTenantById;
using Asn.Diplomski.Domain.Entities;
using Asn.Diplomski.Server.DTOs;
using Microsoft.AspNetCore.Mvc;

namespace Asn.Diplomski.Server.Controllers
{
    /// <summary>
    /// Upravljanje tenantima (kupcima sustava)
    /// </summary>
    [ApiController]
    [Route("api/[controller]")]
    [Produces("application/json")]
    public class TenantsController : ControllerBase
    {
        private readonly CreateTenantHandler _createTenantHandler;
        private readonly GetTenantByIdHandler _getTenantByIdHandler;
        private readonly ILogger<TenantsController> _logger;
        private readonly ConnectTenantHandler _connectTenantHandler;

        public TenantsController(
            CreateTenantHandler createTenantHandler,
            GetTenantByIdHandler getTenantByIdHandler,
            ILogger<TenantsController> logger,
            ConnectTenantHandler connectTenantHandler)
        {
            _createTenantHandler = createTenantHandler;
            _getTenantByIdHandler = getTenantByIdHandler;
            _logger = logger;
            _connectTenantHandler = connectTenantHandler;
        }

        /// <summary>
        /// Kreiranje novog tenanta
        /// </summary>
        /// <param name="request">Podaci novog tenanta</param>
        /// <returns>Kreiran tenant</returns>
        /// <response code="201">Tenant uspješno kreiran</response>
        /// <response code="409">Email već postoji</response>
        /// <response code="400">Neispravni podaci</response>
        [HttpPost]
        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
        {
            var command = new CreateTenantCommand(
                request.Name,
                request.Email,
                request.Password,
                request.Plan,
                request.PhoneNumber,
                request.ContactPersonName,
                request.Street,
                request.City,
                request.PostalCode,
                request.Country);

            var result = await _createTenantHandler.HandleAsync(command);

            if (result.IsEmailConflict)
            {
                _logger.LogWarning(
                    "Kreiranje tenanta odbijeno — email '{Email}' već postoji.", request.Email);
                return Conflict(new { message = $"Email '{request.Email}' već postoji." });
            }

            return CreatedAtAction(
                nameof(GetById),
                new { id = result.Tenant!.Id },
                MapToResponse(result.Tenant!));
        }

        /// <summary>
        /// Dohvat tenanta po ID-u
        /// </summary>
        /// <param name="id">Tenant ID (UUID)</param>
        /// <returns>Tenant info</returns>
        /// <response code="200">Tenant pronađen</response>
        /// <response code="404">Tenant ne postoji</response>
        [HttpGet("{id:long}")]
        [ProducesResponseType(typeof(TenantResponse), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> GetById(long id)
        {
            var tenant = await _getTenantByIdHandler.HandleAsync(id);

            if (tenant is null)
                return NotFound(new { message = $"Tenant s ID-em '{id}' nije pronađen." });

            return Ok(MapToResponse(tenant));
        }

        /// <summary>
        /// Registracija tenantovih uređaja na broker — mikrokontroler poziva ovaj endpoint pri pokretanju.
        /// Server se pretplaćuje na MQTT topic tenantovih uređaja i počinje primati podatke senzora.
        /// </summary>
        /// <param name="id">Device ID</param>
        /// <response code="200">Pretplata aktivna, server sluša poruke uređaja</response>
        /// <response code="404">Uređaj ne postoji</response>
        [HttpPost("{id:long}/connect")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        public async Task<IActionResult> Connect(long id)
        {
            var result = await _connectTenantHandler.HandleAsync(id);

            if (result.IsNotFound)
            {
                _logger.LogWarning(
                    "Connect zahtjev odbijen — uređaj s ID-em {DeviceId} ne postoji.", id);
                return NotFound(new { message = $"Uređaj s ID-em '{id}' ne postoji." });
            }

            return Ok(new
            {
                message = "Pretplata aktivna.",
                topics = result.Topics
            });
        }

        // ── Mapper ──────────────────────────────────────────────

        private static TenantResponse MapToResponse(Tenant t) => new()
        {
            Id = t.Id,
            Name = t.Name,
            Email = t.Email,
            Plan = t.Plan,
            IsActive = t.IsActive,
            CreatedAt = t.CreatedAt,
            UpdatedAt = t.UpdatedAt,
            PhoneNumber = t.PhoneNumber,
            ContactPersonName = t.ContactPersonName,
            Street = t.Street,
            City = t.City,
            PostalCode = t.PostalCode,
            Country = t.Country,
            Devices = t.Devices?.Select(d => new DeviceResponse
            {
                Id           = d.Id,
                Type         = d.Type,
                DeviceNumber = d.DeviceNumber,
                Description  = d.Description,
                IsActive     = d.IsActive,
                CreatedAt    = d.CreatedAt,
                LastSeenAt   = d.LastSeenAt,
                Sensors      = d.Sensors?.Select(s => new SensorResponse
                {
                    Id           = s.Id,
                    Type         = s.Type,
                    SensorNumber = s.SensorNumber,
                    Description  = s.Description,
                    IsActive     = s.IsActive,
                    CreatedAt    = s.CreatedAt,
                }).ToList() ?? []
            }).ToList() ?? []
        };
    }
}
