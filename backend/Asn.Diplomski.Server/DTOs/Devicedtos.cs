using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Server.DTOs
{
    public class DeviceResponse
    {
        public long Id { get; set; }
        public DeviceType Type { get; set; }
        public int DeviceNumber { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? LastSeenAt { get; set; }
        public List<SensorResponse> Sensors { get; set; } = [];
    }

    public class SensorResponse
    {
        public long Id { get; set; }
        public SensorType Type { get; set; }
        public int SensorNumber { get; set; }
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
