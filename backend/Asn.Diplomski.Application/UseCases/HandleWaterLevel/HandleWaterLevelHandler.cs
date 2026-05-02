using Asn.Diplomski.Application.Interfaces;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.HandleWaterLevel
{
    public class HandleWaterLevelHandler
    {
        private readonly IMqttPublisher _mqttPublisher;
        private readonly ILogger<HandleWaterLevelHandler> _logger;

        public HandleWaterLevelHandler(
            IMqttPublisher mqttPublisher,
            ILogger<HandleWaterLevelHandler> logger)
        {
            _mqttPublisher = mqttPublisher;
            _logger = logger;
        }

        public Task HandleAsync(HandleWaterLevelCommand command)
        {
            // TODO: load threshold per device from DB/config
            const double threshold = 20.0;

            if (command.Value < threshold)
            {
                _logger.LogInformation(
                    "Razina vode ispod praga ({Value}% < {Threshold}%) za uređaj {DeviceId} — šaljem komandu pumpi za isključivanje.",
                    command.Value, threshold, command.DeviceId);

                _mqttPublisher.Enqueue(command.TenantId, command.DeviceId, new
                {
                    command = "pump_off",
                    reason = "water_level_low",
                    waterLevel = command.Value,
                    threshold,
                    timestamp = DateTime.UtcNow
                });
            }

            return Task.CompletedTask;
        }
    }
}
