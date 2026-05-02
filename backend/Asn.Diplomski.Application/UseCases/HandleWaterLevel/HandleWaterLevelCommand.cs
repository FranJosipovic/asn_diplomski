namespace Asn.Diplomski.Application.UseCases.HandleWaterLevel
{
    public record HandleWaterLevelCommand(long TenantId, long DeviceId, double Value);
}
