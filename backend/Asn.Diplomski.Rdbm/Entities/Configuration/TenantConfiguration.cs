using Asn.Diplomski.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace Asn.Diplomski.Rdbm.Entities.Configuration
{
    public class TenantConfiguration : IEntityTypeConfiguration<Tenant>
    {
        public void Configure(EntityTypeBuilder<Tenant> builder)
        {
            builder.ToTable("tenants");

            builder.HasKey(t => t.Id);
            builder.Property(t => t.Id)
                .UseIdentityAlwaysColumn();

            builder.Property(t => t.Name)
                .IsRequired()
                .HasMaxLength(200);

            builder.Property(t => t.Email)
                .IsRequired()
                .HasMaxLength(320);
            builder.HasIndex(t => t.Email)
                .IsUnique();

            builder.Property(t => t.Plan)
                .IsRequired()
                .HasMaxLength(50)
                .HasDefaultValue("basic");

            // Contact
            builder.Property(t => t.PhoneNumber)
                .HasMaxLength(30);

            builder.Property(t => t.ContactPersonName)
                .HasMaxLength(200);

            // Address
            builder.Property(t => t.Street)
                .HasMaxLength(300);

            builder.Property(t => t.City)
                .HasMaxLength(100);

            builder.Property(t => t.PostalCode)
                .HasMaxLength(20);

            builder.Property(t => t.Country)
                .HasMaxLength(100);

            // Auth
            builder.Property(t => t.PasswordHash)
                .IsRequired();

            builder.Property(t => t.RefreshToken)
                .HasMaxLength(512);

            builder.Property(t => t.CreatedAt)
                .HasDefaultValueSql("now()");

            builder.Property(t => t.IsActive)
                .HasDefaultValue(true);

            builder.HasMany(t => t.Devices)
                .WithOne(d => d.Tenant)
                .HasForeignKey(d => d.TenantId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
