using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using Asn.Diplomski.Domain.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.CreateDeviceWithSensors
{
    public class CreateDeviceWithSensorsHandler
    {
        private readonly IDeviceRepository _deviceRepository;
        private readonly ILogger<CreateDeviceWithSensorsHandler> _logger;

        public CreateDeviceWithSensorsHandler(
            IDeviceRepository deviceRepository,
            ILogger<CreateDeviceWithSensorsHandler> logger)
        {
            _deviceRepository = deviceRepository;
            _logger = logger;
        }

        /// <summary>
        /// Kreira device i automatski dodaje default senzore ovisno o tipu uređaja.
        /// SensorUnit  → Temperature, SoilMoisture
        /// PumpUnit    → WaterLevel, PumpCommand
        /// </summary>
        public async Task<Device> HandleAsync(CreateDeviceWithSensorsCommand command)
        {
            _logger.LogInformation(
                "Kreiranje uređaja tipa {DeviceType} (broj {DeviceNumber}) za tenant {TenantId}.",
                command.Type, command.DeviceNumber, command.TenantId);

            var device = new Device
            {
                TenantId = command.TenantId,
                Type = command.Type,
                DeviceNumber = command.DeviceNumber,
                DeviceSecret = GenerateDeviceSecret(),
                Description = command.Description,
            };

            device.Sensors = CreateDefaultSensors(command.Type);

            await _deviceRepository.AddAsync(device);

            _logger.LogInformation(
                "Uređaj {DeviceId} ({DeviceType}) kreiran s {SensorCount} senzora za tenant {TenantId}.",
                device.Id, command.Type, device.Sensors.Count, command.TenantId);

            return device;
        }

        private static List<Sensor> CreateDefaultSensors(DeviceType type) => type switch
        {
            DeviceType.SensorUnit => new List<Sensor>
            {
                new() { Type = SensorType.Temperature,  SensorNumber = 1, Description = "Temperatura tla" },
                new() { Type = SensorType.SoilMoisture, SensorNumber = 2, Description = "Vlaga tla" },
            },
            DeviceType.PumpUnit => new List<Sensor>
            {
                new() { Type = SensorType.WaterLevel,  SensorNumber = 1, Description = "Razina vode u spremniku" },
                new() { Type = SensorType.PumpCommand, SensorNumber = 2, Description = "Komanda za pumpu" },
            },
            _ => new List<Sensor>()
        };

        private static string GenerateDeviceSecret()
            => Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
    }
}
