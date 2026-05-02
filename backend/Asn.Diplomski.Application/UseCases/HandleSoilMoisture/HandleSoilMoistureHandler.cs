using Asn.Diplomski.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.HandleSoilMoisture
{
    public class HandleSoilMoistureHandler
    {
        private readonly IMqttPublisher _mqttPublisher;
        private readonly ILogger<HandleSoilMoistureHandler> _logger;

        public HandleSoilMoistureHandler(
            IMqttPublisher mqttPublisher,
            ILogger<HandleSoilMoistureHandler> logger)
        {
            _mqttPublisher = mqttPublisher;
            _logger = logger;
        }

        public Task HandleAsync(HandleSoilMoistureCommand command)
        {
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

            return Task.CompletedTask;
        }
    }
}
