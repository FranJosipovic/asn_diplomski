using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.UseCases.ConnectTenant
{
    public class ConnectTenantResult
    {
        public bool IsNotFound { get; init; }
        public List<Device>? Devices { get; init; }
        public List<string>? Topics { get; init; }

        public static ConnectTenantResult NotFound() => new() { IsNotFound = true };
        public static ConnectTenantResult Success(List<Device> devices, List<string> topics)
            => new() { Devices = devices, Topics = topics };
    }
}
