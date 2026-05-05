namespace Asn.Diplomski.Application.UseCases.HandleSoilMoisture
{
    public record HandleSoilMoistureCommand(long TenantId, long DeviceId, long SensorId, double Value);
}
