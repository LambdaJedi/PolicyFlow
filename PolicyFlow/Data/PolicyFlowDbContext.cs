using Microsoft.EntityFrameworkCore;
using PolicyFlow.Models;

namespace PolicyFlow.Data;

public class PolicyFlowDbContext : DbContext
{
    public PolicyFlowDbContext(DbContextOptions<PolicyFlowDbContext> options)
        : base(options)
    {
    }

    public DbSet<InsurancePolicy> Policies => Set<InsurancePolicy>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<InsurancePolicy>(entity =>
        {
            entity.Property(p => p.MonthlyPremium).HasPrecision(18, 2);

            // Enforced by the database so two simultaneous requests
            // can't both create the same policy number.
            entity.HasIndex(p => p.PolicyNumber).IsUnique();
        });
    }
}
