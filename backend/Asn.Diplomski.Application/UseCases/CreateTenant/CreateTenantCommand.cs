namespace Asn.Diplomski.Application.UseCases.CreateTenant
{
    public record CreateTenantCommand(
        string Name,
        string Email,
        string Password,
        string Plan,
        string? PhoneNumber,
        string? ContactPersonName,
        string? Street,
        string? City,
        string? PostalCode,
        string? Country);
}
