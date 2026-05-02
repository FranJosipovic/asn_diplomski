using Asn.Diplomski.Application.Interfaces;
using Asn.Diplomski.Rdbm.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

namespace Asn.Diplomski.Rdbm
{
    public static class RdbmServiceExtensions
    {
        public static IServiceCollection AddRdbm(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddDbContext<AsnDbContext>(options =>
                options.UseNpgsql(
                    configuration.GetConnectionString("Postgres"),
                    npgsql => npgsql.MigrationsAssembly(typeof(AsnDbContext).Assembly.FullName)
                ));

            services.AddScoped<IDeviceRepository, DeviceRepository>();
            services.AddScoped<ITenantRepository, TenantRepository>();

            return services;
        }
    }
}
