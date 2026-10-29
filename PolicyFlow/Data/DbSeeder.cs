using Microsoft.EntityFrameworkCore;
using PolicyFlow.Models;

namespace PolicyFlow.Data;

/// <summary>Inserts sample data so a fresh install isn't an empty dashboard.</summary>
public static class DbSeeder
{
    public static async Task SeedAsync(PolicyFlowDbContext context)
    {
        if (await context.Policies.AnyAsync())
        {
            return;
        }

        context.Policies.AddRange(
            new InsurancePolicy { PolicyNumber = "POL-1001", CustomerName = "Thandi Mokoena", PolicyType = PolicyTypes.Vehicle, MonthlyPremium = 850m, Status = PolicyStatuses.Active },
            new InsurancePolicy { PolicyNumber = "POL-1002", CustomerName = "Pieter van der Merwe", PolicyType = PolicyTypes.Home, MonthlyPremium = 1450m, Status = PolicyStatuses.Active },
            new InsurancePolicy { PolicyNumber = "POL-1003", CustomerName = "Aisha Patel", PolicyType = PolicyTypes.Life, MonthlyPremium = 620m, Status = PolicyStatuses.Active },
            new InsurancePolicy { PolicyNumber = "POL-1004", CustomerName = "Sipho Dlamini", PolicyType = PolicyTypes.Vehicle, MonthlyPremium = 990m, Status = PolicyStatuses.Pending },
            new InsurancePolicy { PolicyNumber = "POL-1005", CustomerName = "Karen Jacobs", PolicyType = PolicyTypes.Home, MonthlyPremium = 1780m, Status = PolicyStatuses.Cancelled });

        await context.SaveChangesAsync();
    }
}
