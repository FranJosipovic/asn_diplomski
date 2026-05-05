using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.Interfaces
{
    public interface ITemperatureReadingRepository
    {
        Task AddAsync(TemperatureReading reading);

        Task<IReadOnlyList<TemperatureReading>> GetForSensorSinceAsync(
            long sensorId,
            DateTime sinceUtc);

        Task<IReadOnlyList<TemperatureReading>> GetForSensorLastAsync(
            long sensorId,
            TimeSpan window);

        Task<TemperatureReading?> GetLatestForSensorAsync(long sensorId);
    }
}
