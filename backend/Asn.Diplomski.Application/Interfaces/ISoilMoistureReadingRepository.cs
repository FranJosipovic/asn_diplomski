using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.Interfaces
{
    public interface ISoilMoistureReadingRepository
    {
        Task AddAsync(SoilMoistureReading reading);

        Task<IReadOnlyList<SoilMoistureReading>> GetForSensorSinceAsync(
            long sensorId,
            DateTime sinceUtc);

        Task<IReadOnlyList<SoilMoistureReading>> GetForSensorLastAsync(
            long sensorId,
            TimeSpan window);

        Task<SoilMoistureReading?> GetLatestForSensorAsync(long sensorId);
    }
}
