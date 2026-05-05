using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.HandleTemperature
{
    public class HandleTemperatureHandler
    {
        private readonly IMqttPublisher _mqttPublisher;
        private readonly ILogger<HandleTemperatureHandler> _logger;
        private readonly ITemperatureReadingRepository _temperatureReadingRepository;

        public HandleTemperatureHandler(
            IMqttPublisher mqttPublisher,
            ILogger<HandleTemperatureHandler> logger,
            ITemperatureReadingRepository temperatureReadingRepository)
        {
            _mqttPublisher = mqttPublisher;
            _logger = logger;
            _temperatureReadingRepository = temperatureReadingRepository;
        }

        public async Task HandleAsync(HandleTemperatureCommand command)
        {

            //Insert into db
            var reading = new TemperatureReading
            {
                SensorId = command.SensorId,
                Value = command.Value,
                RecordedAt = DateTime.UtcNow
            };

            await _temperatureReadingRepository.AddAsync(reading);
        }
    }
}
