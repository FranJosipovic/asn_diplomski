using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace Asn.Diplomski.Rdbm
{
    public class AsnDbContextFactory : IDesignTimeDbContextFactory<AsnDbContext>
    {
        public AsnDbContext CreateDbContext(string[] args)
        {
            var optionsBuilder = new DbContextOptionsBuilder<AsnDbContext>();

            optionsBuilder.UseNpgsql(
                "Host=localhost;Port=5434;Database=asn_db;Username=asn_usr;Password=asn_pass");

            return new AsnDbContext(optionsBuilder.Options);
        }
    }
}
