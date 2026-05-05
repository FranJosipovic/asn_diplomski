using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using Asn.Diplomski.Domain.Entities.Enums;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.HandleSoilMoisture
{
    public class HandleSoilMoistureHandler
    {
        private readonly IMqttPublisher _mqttPublisher;
        private readonly ILogger<HandleSoilMoistureHandler> _logger;
        private readonly ISoilMoistureReadingRepository _soilMoistureReadingRepository;
        private readonly IDeviceRepository _deviceRepository;

        public HandleSoilMoistureHandler(
            IMqttPublisher mqttPublisher,
            ILogger<HandleSoilMoistureHandler> logger,
            ISoilMoistureReadingRepository soilMoistureReadingRepository,
            IDeviceRepository deviceRepository)
        {
            _mqttPublisher = mqttPublisher;
            _logger = logger;
            _soilMoistureReadingRepository = soilMoistureReadingRepository;
            _deviceRepository = deviceRepository;
        }

        public async Task HandleAsync(HandleSoilMoistureCommand command)
        {
            //Insert into db
            var reading = new SoilMoistureReading
            {
                SensorId = command.SensorId,
                Value = command.Value,
                RecordedAt = DateTime.UtcNow
            };

            await _soilMoistureReadingRepository.AddAsync(reading);

            // TODO: load threshold per device from DB/config
            const double threshold = 30.0;

            if (command.Value < threshold)
            {
                _logger.LogInformation(
                    "Vlaga tla ispod praga ({Value}% < {Threshold}%) za uređaj {DeviceId} — šaljem komandu pumpi.",
                    command.Value, threshold, command.DeviceId);

                _mqttPublisher.Enqueue(command.TenantId, command.DeviceId, new
                {
                    command = "pump_on",
                    reason = "soil_moisture_low",
                    soilMoisture = command.Value,
                    threshold,
                    timestamp = DateTime.UtcNow
                });
            }
        }
    }
}
