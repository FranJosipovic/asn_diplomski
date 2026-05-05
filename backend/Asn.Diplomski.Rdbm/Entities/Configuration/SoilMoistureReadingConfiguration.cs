using Asn.Diplomski.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asn.Diplomski.Rdbm.Entities.Configuration
{
    public class SoilMoistureReadingConfiguration : IEntityTypeConfiguration<SoilMoistureReading>
    {
        public void Configure(EntityTypeBuilder<SoilMoistureReading> builder)
        {
            builder.ToTable("soil_moisture_readings");

            builder.HasKey(r => r.Id);
            builder.Property(r => r.Id)
                .UseIdentityAlwaysColumn();

            builder.Property(r => r.Value)
                .IsRequired()
                .HasPrecision(5, 2);

            builder.Property(r => r.RecordedAt)
                .IsRequired();

            builder.HasIndex(r => new { r.SensorId, r.RecordedAt });

            builder.HasOne(r => r.Sensor)
                .WithMany(s => s.SoilMoistureReadings)
                .HasForeignKey(r => r.SensorId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
