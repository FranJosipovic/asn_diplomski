using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Application.UseCases.CreateTenant;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Server
{
    public static class DbSeeder
    {
        public static async Task SeedAsync(
            CreateTenantHandler createTenantHandler,
            ITenantRepository tenantRepository,
            ILogger logger)
        {
            if (await tenantRepository.HasAnyAsync())
            {
                logger.LogInformation("Seed preskočen — baza već sadrži podatke.");
                return;
            }

            logger.LogInformation("Pokretanje seed-a baze podataka...");

            var result = await createTenantHandler.HandleAsync(new CreateTenantCommand(
                Name: "Test Klijent d.o.o.",
                Email: "test@diplomski.hr",
                Password: "Test1234!",
                Plan: "pro",
                PhoneNumber: "+385 91 000 0000",
                ContactPersonName: "Ivan Horvat",
                Street: "Ulica testna 1",
                City: "Zagreb",
                PostalCode: "10000",
                Country: "Hrvatska"));

            var tenant = result.Tenant!;

            logger.LogInformation(
                "Seed: kreiran tenant '{Name}' (ID: {TenantId}).",
                tenant.Name, tenant.Id);

            var devices = tenant.Devices.ToList();

            logger.LogInformation(
                "Seed: kreiran SensorUnit (ID: {DeviceId}) za tenant {TenantId}.",
                devices[0].Id, tenant.Id);

            logger.LogInformation(
                "Seed: kreiran PumpUnit (ID: {DeviceId}) za tenant {TenantId}.",
                devices[1].Id, tenant.Id);

            logger.LogInformation(
                "Seed završen — kreiran 1 tenant s {DeviceCount} uređaja.",
                2);
        }
    }
}
