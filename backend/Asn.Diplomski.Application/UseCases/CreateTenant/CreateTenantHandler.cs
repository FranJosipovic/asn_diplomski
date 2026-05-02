using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Application.UseCases.CreateDeviceWithSensors;
using Asn.Diplomski.Domain.Entities;
using Asn.Diplomski.Domain.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.CreateTenant
{
    public class CreateTenantHandler
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly CreateDeviceWithSensorsHandler _createDeviceHandler;
        private readonly ILogger<CreateTenantHandler> _logger;

        public CreateTenantHandler(
            ITenantRepository tenantRepository,
            CreateDeviceWithSensorsHandler createDeviceHandler,
            ILogger<CreateTenantHandler> logger)
        {
            _tenantRepository = tenantRepository;
            _createDeviceHandler = createDeviceHandler;
            _logger = logger;
        }

        public async Task<CreateTenantResult> HandleAsync(CreateTenantCommand command)
        {
            _logger.LogInformation(
                "Zahtjev za kreiranje tenanta s emailom '{Email}'.", command.Email);

            if (await _tenantRepository.EmailExistsAsync(command.Email))
            {
                _logger.LogWarning(
                    "Kreiranje tenanta odbijeno — email '{Email}' već postoji.", command.Email);
                return CreateTenantResult.EmailConflict();
            }

            var tenant = new Tenant
            {
                Name = command.Name,
                Email = command.Email.ToLower(),
                Plan = command.Plan,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(command.Password),
                PhoneNumber = command.PhoneNumber,
                ContactPersonName = command.ContactPersonName,
                Street = command.Street,
                City = command.City,
                PostalCode = command.PostalCode,
                Country = command.Country,
            };

            await _tenantRepository.AddAsync(tenant);

            _logger.LogInformation(
                "Tenant '{Name}' (ID: {TenantId}) uspješno kreiran.",
                tenant.Name, tenant.Id);

            var sensorUnit = await _createDeviceHandler.HandleAsync(
                new CreateDeviceWithSensorsCommand(tenant.Id, DeviceType.SensorUnit, 1, null));
            var pumpUnit = await _createDeviceHandler.HandleAsync(
                new CreateDeviceWithSensorsCommand(tenant.Id, DeviceType.PumpUnit, 2, null));

            tenant.Devices.Add(sensorUnit);
            tenant.Devices.Add(pumpUnit);

            _logger.LogInformation(
                "Provisionirana 2 uređaja za tenant {TenantId}: SensorUnit={SensorUnitId}, PumpUnit={PumpUnitId}.",
                tenant.Id, sensorUnit.Id, pumpUnit.Id);

            return CreateTenantResult.Success(tenant);
        }
    }
}
