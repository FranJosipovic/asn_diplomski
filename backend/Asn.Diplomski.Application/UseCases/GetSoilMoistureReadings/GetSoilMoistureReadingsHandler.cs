using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.UseCases.GetSoilMoistureReadings
{
    public class GetSoilMoistureReadingsHandler(ISoilMoistureReadingRepository repository)
    {
        public Task<IReadOnlyList<SoilMoistureReading>> GetHistoryAsync(long sensorId, TimeSpan window)
            => repository.GetForSensorLastAsync(sensorId, window);

        public Task<SoilMoistureReading?> GetLatestAsync(long sensorId)
            => repository.GetLatestForSensorAsync(sensorId);
    }
}
