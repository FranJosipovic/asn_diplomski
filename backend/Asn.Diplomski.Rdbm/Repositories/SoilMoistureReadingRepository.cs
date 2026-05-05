using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Asn.Diplomski.Rdbm.Repositories
{
    public class SoilMoistureReadingRepository : ISoilMoistureReadingRepository
    {
        private readonly AsnDbContext _db;

        public SoilMoistureReadingRepository(AsnDbContext db)
        {
            _db = db;
        }

        public async Task AddAsync(SoilMoistureReading reading)
        {
            _db.SoilMoistureReadings.Add(reading);
            await _db.SaveChangesAsync();
        }

        public async Task<IReadOnlyList<SoilMoistureReading>> GetForSensorSinceAsync(
            long sensorId,
            DateTime sinceUtc)
        {
            return await _db.SoilMoistureReadings
                .Where(r => r.SensorId == sensorId && r.RecordedAt >= sinceUtc)
                .OrderBy(r => r.RecordedAt)
                .ToListAsync();
        }

        public Task<IReadOnlyList<SoilMoistureReading>> GetForSensorLastAsync(
            long sensorId,
            TimeSpan window)
        {
            var sinceUtc = DateTime.UtcNow - window;
            return GetForSensorSinceAsync(sensorId, sinceUtc);
        }

        public async Task<SoilMoistureReading?> GetLatestForSensorAsync(long sensorId)
        {
            return await _db.SoilMoistureReadings
                .Where(r => r.SensorId == sensorId)
                .OrderByDescending(r => r.RecordedAt)
                .FirstOrDefaultAsync();
        }
    }
}
