using Asn.Diplomski.Domain.Entities.Enums;

namespace Asn.Diplomski.Domain.Entities
{
    public class Sensor
    {
        public long Id { get; set; }

        public long DeviceId { get; set; }

        public SensorType Type { get; set; }

        /// <summary>
        /// Redni broj senzora na uređaju (1, 2...)
        /// Koristi se za MQTT topic: tenant/device_n/sensor/type
        /// </summary>
        public int SensorNumber { get; set; }

        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; }

        // Navigation
        public Device Device { get; set; } = null!;
    }
}
