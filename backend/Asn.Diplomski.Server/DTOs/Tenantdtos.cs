using System.ComponentModel.DataAnnotations;

namespace Asn.Diplomski.Server.DTOs
{
    // ── Request ──────────────────────────────────────────────────

    public class CreateTenantRequest
    {
        [Required, MaxLength(200)]
        public string Name { get; set; } = null!;

        [Required, EmailAddress, MaxLength(320)]
        public string Email { get; set; } = null!;

        [Required, MinLength(8), MaxLength(100)]
        public string Password { get; set; } = null!;

        [MaxLength(50)]
        public string Plan { get; set; } = "basic";

        // Contact (opcionalno)
        [MaxLength(30)] public string? PhoneNumber { get; set; }
        [MaxLength(200)] public string? ContactPersonName { get; set; }

        // Address (opcionalno)
        [MaxLength(300)] public string? Street { get; set; }
        [MaxLength(100)] public string? City { get; set; }
        [MaxLength(20)] public string? PostalCode { get; set; }
        [MaxLength(100)] public string? Country { get; set; }
    }

    // ── Response ─────────────────────────────────────────────────

    public class TenantResponse
    {
        public long Id { get; set; }
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string Plan { get; set; } = null!;
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        // Contact
        public string? PhoneNumber { get; set; }
        public string? ContactPersonName { get; set; }

        // Address
        public string? Street { get; set; }
        public string? City { get; set; }
        public string? PostalCode { get; set; }
        public string? Country { get; set; }

        // Devices
        public List<DeviceResponse> Devices { get; set; } = [];
    }
}
