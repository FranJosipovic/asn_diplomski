using Asn.Diplomski.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asn.Diplomski.Rdbm.Entities.Configuration
{
    public class SensorConfiguration : IEntityTypeConfiguration<Sensor>
    {
        public void Configure(EntityTypeBuilder<Sensor> builder)
        {
            builder.ToTable("sensors");

            builder.HasKey(s => s.Id);
            builder.Property(s => s.Id)
                .UseIdentityAlwaysColumn();

            builder.Property(s => s.Type)
                .IsRequired()
                .HasConversion<int>();

            builder.Property(s => s.SensorNumber)
                .IsRequired();

            // device_id + type mora biti unikatan
            builder.HasIndex(s => new { s.DeviceId, s.Type })
                .IsUnique();

            builder.Property(s => s.Description)
                .HasMaxLength(500);

            builder.Property(s => s.IsActive)
                .HasDefaultValue(true);

            builder.Property(s => s.CreatedAt)
                .HasDefaultValueSql("now()");

            builder.HasOne(s => s.Device)
                .WithMany(d => d.Sensors)
                .HasForeignKey(s => s.DeviceId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
