namespace Asn.Diplomski.Server.DTOs
{
    public class ReadingResponse
    {
        public long Id { get; set; }
        public double Value { get; set; }
        public DateTime RecordedAt { get; set; }
    }

    public class ReadingHistoryResponse
    {
        public long SensorId { get; set; }
        public string Unit { get; set; } = null!;
        public List<ReadingResponse> Readings { get; set; } = [];
    }
}
