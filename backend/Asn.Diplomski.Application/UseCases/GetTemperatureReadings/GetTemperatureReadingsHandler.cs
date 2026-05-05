using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.UseCases.GetTemperatureReadings
{
    public class GetTemperatureReadingsHandler(ITemperatureReadingRepository repository)
    {
        public Task<IReadOnlyList<TemperatureReading>> GetHistoryAsync(long sensorId, TimeSpan window)
            => repository.GetForSensorLastAsync(sensorId, window);

        public Task<TemperatureReading?> GetLatestAsync(long sensorId)
            => repository.GetLatestForSensorAsync(sensorId);
    }
}
