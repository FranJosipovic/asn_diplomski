namespace Asn.Diplomski.Application.UseCases.SetDeviceProvisioningReady
{
    public class SetDeviceProvisioningReadyResult
    {
        public bool IsNotFound { get; private init; }
        public string? ProvisioningToken { get; private init; }
        public DateTime ExpiresAt { get; private init; }

        public static SetDeviceProvisioningReadyResult NotFound() => new() { IsNotFound = true };

        public static SetDeviceProvisioningReadyResult Success(string token, DateTime expiresAt) =>
            new() { ProvisioningToken = token, ExpiresAt = expiresAt };
    }
}
