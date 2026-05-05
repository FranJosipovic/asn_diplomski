using Asn.Diplomski.Domain.Entities;
using Asn.Diplomski.Rdbm.Entities.Configuration;
using Microsoft.EntityFrameworkCore;

namespace Asn.Diplomski.Rdbm
{
    public class AsnDbContext : DbContext
    {
        public AsnDbContext(DbContextOptions<AsnDbContext> options) : base(options) { }

        public DbSet<Tenant> Tenants => Set<Tenant>();
        public DbSet<Device> Devices => Set<Device>();
        public DbSet<Sensor> Sensors => Set<Sensor>();
        public DbSet<TemperatureReading> TemperatureReadings => Set<TemperatureReading>();
        public DbSet<SoilMoistureReading> SoilMoistureReadings => Set<SoilMoistureReading>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.ApplyConfiguration(new TenantConfiguration());
            modelBuilder.ApplyConfiguration(new DeviceConfiguration());
            modelBuilder.ApplyConfiguration(new SensorConfiguration());
            modelBuilder.ApplyConfiguration(new TemperatureReadingConfiguration());
            modelBuilder.ApplyConfiguration(new SoilMoistureReadingConfiguration());
        }

        public override Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SetTimestamps();
            return base.SaveChangesAsync(cancellationToken);
        }

        public override int SaveChanges()
        {
            SetTimestamps();
            return base.SaveChanges();
        }

        private void SetTimestamps()
        {
            var now = DateTime.UtcNow;

            foreach (var entry in ChangeTracker.Entries<Tenant>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = now;
                if (entry.State == EntityState.Modified)
                    entry.Entity.UpdatedAt = now;
            }

            foreach (var entry in ChangeTracker.Entries<Device>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = now;
            }

            foreach (var entry in ChangeTracker.Entries<Sensor>())
            {
                if (entry.State == EntityState.Added)
                    entry.Entity.CreatedAt = now;
            }
        }
    }
}
