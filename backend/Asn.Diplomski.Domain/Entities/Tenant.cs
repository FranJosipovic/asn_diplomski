namespace Asn.Diplomski.Domain.Entities
{
    public class Tenant
    {
        public long Id { get; set; }

        // Basic info
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Plan { get; set; } = null!;   // e.g. "basic", "pro"

        // Contact
        public string? PhoneNumber { get; set; }
        public string? ContactPersonName { get; set; }

        // Address
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // Auth
        public string PasswordHash { get; set; } = null!;
        public string? RefreshToken { get; set; }
        public DateTime? RefreshTokenExpiresAt { get; set; }

        // Metadata
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public bool IsActive { get; set; } = true;

        // Navigation
        public ICollection<Device> Devices { get; set; } = new List<Device>();
    }
}
