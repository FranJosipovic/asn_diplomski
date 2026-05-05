namespace Asn.Diplomski.Application.UseCases.HandleTemperature
{
    public record HandleTemperatureCommand(long TenantId, long DeviceId,long SensorId, double Value);
}
