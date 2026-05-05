namespace Asn.Diplomski.Domain.Entities
{
    public class SoilMoistureReading
    {
        public long Id { get; set; }

        public long SensorId { get; set; }

        /// <summary>
        /// Percentage (0–100)
        /// </summary>
        public double Value { get; set; }

        public DateTime RecordedAt { get; set; }

        // Navigation
        public Sensor Sensor { get; set; } = null!;
    }
}
