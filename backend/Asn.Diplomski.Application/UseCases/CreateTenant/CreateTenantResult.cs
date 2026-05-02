using Asn.Diplomski.Domain.Entities;

namespace Asn.Diplomski.Application.UseCases.CreateTenant
{
    public class CreateTenantResult
    {
        public bool IsEmailConflict { get; init; }
        public Tenant? Tenant { get; init; }

        public static CreateTenantResult EmailConflict() => new() { IsEmailConflict = true };
        public static CreateTenantResult Success(Tenant tenant) => new() { Tenant = tenant };
    }
}
