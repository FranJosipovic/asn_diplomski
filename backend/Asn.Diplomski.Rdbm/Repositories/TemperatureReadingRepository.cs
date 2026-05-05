using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Asn.Diplomski.Rdbm.Repositories
{
    public class TemperatureReadingRepository : ITemperatureReadingRepository
    {
        private readonly AsnDbContext _db;

        public TemperatureReadingRepository(AsnDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(TemperatureReading reading)
        {
            _db.TemperatureReadings.Add(reading);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<TemperatureReading>> GetForSensorSinceAsync(
            long sensorId,
            DateTime sinceUtc)
        {
            return await _db.TemperatureReadings
                .Where(r => r.SensorId == sensorId && r.RecordedAt >= sinceUtc)
                .OrderBy(r => r.RecordedAt)
                .ToListAsync();
        }

        public Task<IReadOnlyList<TemperatureReading>> GetForSensorLastAsync(
            long sensorId,
            TimeSpan window)
        {
            var sinceUtc = DateTime.UtcNow - window;
            return GetForSensorSinceAsync(sensorId, sinceUtc);
        }

        public async Task<TemperatureReading?> GetLatestForSensorAsync(long sensorId)
        {
            return await _db.TemperatureReadings
                .Where(r => r.SensorId == sensorId)
                .OrderByDescending(r => r.RecordedAt)
                .FirstOrDefaultAsync();
        }
    }
}
