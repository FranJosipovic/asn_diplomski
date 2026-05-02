using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Domain.Entities
{
    public class Device
    {
        public long Id { get; set; }

        public long TenantId { get; set; }

        public DeviceType Type { get; set; }

        // Identifikator koji ide na mikrokontroler
        public string DeviceSecret { get; set; } = null!;

        // Redni broj uređaja kod klijenta (1, 2...)
        public int DeviceNumber { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }

        // Navigation
        public Tenant Tenant { get; set; } = null!;
        public ICollection<Sensor> Sensors { get; set; } = new List<Sensor>();
    }
}
