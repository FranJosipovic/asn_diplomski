using Asn.Diplomski.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asn.Diplomski.Rdbm.Entities.Configuration
{
    public class DeviceConfiguration : IEntityTypeConfiguration<Device>
    {
        public void Configure(EntityTypeBuilder<Device> builder)
        {
            builder.ToTable("devices");

            builder.HasKey(d => d.Id);
            builder.Property(d => d.Id)
                .UseIdentityAlwaysColumn();

            builder.Property(d => d.DeviceSecret)
                .IsRequired()
                .HasMaxLength(256);
            builder.HasIndex(d => d.DeviceSecret)
                .IsUnique();

            builder.Property(d => d.DeviceNumber)
                .IsRequired();

            // tenant_id + device_number mora biti unikatan
            builder.HasIndex(d => new { d.TenantId, d.DeviceNumber })
                .IsUnique();

            builder.Property(d => d.Description)
                .HasMaxLength(500);

            builder.Property(d => d.IsActive)
                .HasDefaultValue(true);

            builder.Property(d => d.CreatedAt)
                .HasDefaultValueSql("now()");
        }
    }
}
