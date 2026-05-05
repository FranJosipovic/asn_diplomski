namespace Asn.Diplomski.Domain.Entities
{
    public class TemperatureReading
    {
        public long Id { get; set; }

        public long SensorId { get; set; }

        public double Value { get; set; }

        public DateTime RecordedAt { get; set; }

        // Navigation
        public Sensor Sensor { get; set; } = null!;
    }
}
