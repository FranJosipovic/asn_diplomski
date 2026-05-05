namespace Asn.Diplomski.Application.UseCases.HandleWaterLevel
{
    public record HandleWaterLevelCommand(long TenantId, long DeviceId,long SensorId, double Value);
}
