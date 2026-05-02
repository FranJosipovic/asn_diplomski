using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using Microsoft.EntityFrameworkCore;

namespace Asn.Diplomski.Rdbm.Repositories
{
    public class TenantRepository : ITenantRepository
    {
        private readonly AsnDbContext _db;

        public TenantRepository(AsnDbContext db)
        {
            _db = db;
        }

        public Task<bool> HasAnyAsync()
            => _db.Tenants.AnyAsync();

        public Task<bool> EmailExistsAsync(string email)
            => _db.Tenants.AnyAsync(t => t.Email == email.ToLower());

        public Task<Tenant?> GetByIdWithDevicesAndSensorsAsync(long id)
            => _db.Tenants
                .Include(t => t.Devices)
                    .ThenInclude(d => d.Sensors)
                .FirstOrDefaultAsync(t => t.Id == id);

        public async Task AddAsync(Tenant tenant)
        {
            _db.Tenants.Add(tenant);
            await _db.SaveChangesAsync();
        }
    }
}
