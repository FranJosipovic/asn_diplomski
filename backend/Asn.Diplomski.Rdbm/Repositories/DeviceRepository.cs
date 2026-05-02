using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Asn.Diplomski.Rdbm.Repositories
{
    public class DeviceRepository : IDeviceRepository
    {
        private readonly AsnDbContext _db;

        public DeviceRepository(AsnDbContext db)
        {
            _db = db;
        }

        public Task<Device?> GetByIdWithSensorsAsync(long id)
            => _db.Devices
                .Include(d => d.Sensors)
                .FirstOrDefaultAsync(d => d.Id == id);

        public async Task<IReadOnlyList<Device>> GetAllActiveWithSensorsAsync()
            => await _db.Devices
                .Include(d => d.Sensors)
                .Where(d => d.IsActive)
                .ToListAsync();

        public async Task AddAsync(Device device)
        {
            _db.Devices.Add(device);
            await _db.SaveChangesAsync();
        }

        public async Task UpdateAsync(Device device)
        {
            _db.Devices.Update(device);
            await _db.SaveChangesAsync();
        }
    }
}
