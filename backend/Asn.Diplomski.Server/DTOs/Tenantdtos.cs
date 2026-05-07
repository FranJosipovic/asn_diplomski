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

    public class SignInRequestDto
    {
        [Required, EmailAddress, MaxLength(320)]
        public string Email { get; set; } = null!;

        [Required, MinLength(8), MaxLength(100)]
        public string Password { get; set; } = null!;
    }

    // ── Response ─────────────────────────────────────────────────

    public class SignInResponseDto
    {
        public long TenantId { get; set; }
        public string Email { get; set; } = null!;
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }

    public class RefreshTokenRequestDto
    {
        [Required]
        public string RefreshToken { get; set; } = null!;
    }

    public class RefreshTokenResponseDto
    {
        public string AccessToken { get; set; } = null!;
        public string RefreshToken { get; set; } = null!;
        public DateTime RefreshTokenExpiresAt { get; set; }
    }

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

    public class ProvisioningTokenResponseDto
    {
        public DateTime ExpiresAt { get; set; }
        public string ServerHost { get; set; } = null!;
        public int ServerPort { get; set; }
        public List<DeviceProvisioningDto> Devices { get; set; } = [];
    }

    public class DeviceProvisioningDto
    {
        public long DeviceId { get; set; }
        public string DeviceType { get; set; } = null!;
        public string DeviceSsid { get; set; } = null!;
        public string ProvisionStatus { get; set; } = null!;
        public string ProvisioningToken { get; set; } = null!;
        public List<SensorProvisioningDto> Sensors { get; set; } = [];
    }

    public class SensorProvisioningDto
    {
        public long SensorId { get; set; }
        public string SensorType { get; set; } = null!;
    }

    public class CompleteProvisioningResponseDto
    {
        public long TenantId { get; set; }
        public long DeviceId { get; set; }
        public string MqttHost { get; set; } = null!;
        public int MqttPort { get; set; }
        public List<SensorProvisioningDto> Sensors { get; set; } = [];
    }

    public class CompleteProvisioningRequestDto
    {
        public string Token { get; set; } = null!;
    }

    public class ConfirmMqttConnectionRequestDto
    {
        public long TenantId { get; set; }
        public long DeviceId { get; set; }
    }

    public class StartSystemResponseDto
    {
        public int NotifiedDeviceCount { get; set; }
        public List<long> NotifiedDeviceIds { get; set; } = [];
    }
}
