using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using Asn.Diplomski.Domain.Entities.Enums;
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

        public async Task UpdateRangeAsync(IList<Device> devices)
        {
            _db.Devices.UpdateRange(devices);
            await _db.SaveChangesAsync();
        }

        public Task<Device?> GetByProvisioningTokenAsync(string token)
            => _db.Devices
                .Include(d => d.Sensors)
                .FirstOrDefaultAsync(d => d.ProvisioningToken == token
                    && d.ProvisioningTokenExpiresAt > DateTime.UtcNow);

        public Task<Device?> GetByProvisioningTokenIgnoringExpiryAsync(string token)
            => _db.Devices
                .FirstOrDefaultAsync(d => d.ProvisioningToken == token);

        public Task<Device?> GetByIdAndTenantAsync(long deviceId, long tenantId)
            => _db.Devices
                .FirstOrDefaultAsync(d => d.Id == deviceId && d.TenantId == tenantId);

        public async Task<IReadOnlyList<Device>> GetAllProvisionedByTenantAsync(long tenantId)
            => await _db.Devices
                .Where(d => d.TenantId == tenantId
                    && d.ProvisionStatus == ProvisionStatus.Provisioned
                    && d.IsActive)
                .ToListAsync();
    }
}
