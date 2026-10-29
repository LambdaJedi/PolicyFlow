using Microsoft.EntityFrameworkCore;
using PolicyFlow.Data;

namespace PolicyFlow.Tests;

/// <summary>Creates contexts that all share one isolated in-memory database.</summary>
internal sealed class TestDbContextFactory : IDbContextFactory<PolicyFlowDbContext>
{
    private readonly DbContextOptions<PolicyFlowDbContext> _options =
        new DbContextOptionsBuilder<PolicyFlowDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

    public PolicyFlowDbContext CreateDbContext() => new(_options);
}
