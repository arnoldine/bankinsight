using HybridTransfer.Infrastructure.Persistence;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace HybridTransfer.Infrastructure;

public sealed class HybridTransferDbContextFactory : IDesignTimeDbContextFactory<HybridTransferDbContext>
{
    public HybridTransferDbContext CreateDbContext(string[] args)
    {
        var connectionString = "Host=localhost;Port=5432;Database=hybridtransfer;Username=hybridtransfer;Password=change-me";
        var optionsBuilder = new DbContextOptionsBuilder<HybridTransferDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        return new HybridTransferDbContext(optionsBuilder.Options);
    }
}
