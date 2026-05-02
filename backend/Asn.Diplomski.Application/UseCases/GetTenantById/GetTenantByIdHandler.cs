using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Domain.Entities;
using Microsoft.Extensions.Logging;

namespace Asn.Diplomski.Application.UseCases.GetTenantById
{
    public class GetTenantByIdHandler
    {
        private readonly ITenantRepository _tenantRepository;
        private readonly ILogger<GetTenantByIdHandler> _logger;

        public GetTenantByIdHandler(
            ITenantRepository tenantRepository,
            ILogger<GetTenantByIdHandler> logger)
        {
            _tenantRepository = tenantRepository;
            _logger = logger;
        }

        public async Task<Tenant?> HandleAsync(long id)
        {
            _logger.LogInformation("Dohvat tenanta s ID-em {TenantId}.", id);

            var tenant = await _tenantRepository.GetByIdWithDevicesAndSensorsAsync(id);

            if (tenant is null)
                _logger.LogWarning("Tenant s ID-em {TenantId} nije pronađen.", id);
            else
                _logger.LogInformation(
                    "Tenant {TenantId} pronađen — '{Name}', {DeviceCount} uređaja.",
                    id, tenant.Name, tenant.Devices.Count);

            return tenant;
        }
    }
}
