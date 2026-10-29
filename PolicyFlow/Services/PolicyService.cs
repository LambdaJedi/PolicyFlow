using Microsoft.EntityFrameworkCore;
using PolicyFlow.Data;
using PolicyFlow.Models;

namespace PolicyFlow.Services;

/// <summary>Outcome of a create or update operation.</summary>
public enum PolicyResult
{
    Success,
    NotFound,
    DuplicatePolicyNumber,
    InvalidPolicyType,
    InvalidStatus
}

public static class PolicyResultExtensions
{
    /// <summary>A user-friendly message for a failed result.</summary>
    public static string ToMessage(this PolicyResult result) => result switch
    {
        PolicyResult.Success => "",
        PolicyResult.NotFound => "The policy could not be found. It may have been deleted.",
        PolicyResult.DuplicatePolicyNumber => "That policy number already exists. Please use a different number.",
        PolicyResult.InvalidPolicyType => "Please choose a valid policy type.",
        PolicyResult.InvalidStatus => "Please choose a valid status.",
        _ => "Something went wrong. Please try again."
    };
}

public class PolicyService
{
    // Blazor Server circuits live a long time, so a shared DbContext would
    // accumulate tracked entities. A short-lived context per operation avoids that.
    private readonly IDbContextFactory<PolicyFlowDbContext> _contextFactory;

    public PolicyService(IDbContextFactory<PolicyFlowDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<List<InsurancePolicy>> GetAllAsync()
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Policies
            .AsNoTracking()
            .OrderBy(p => p.Id)
            .ToListAsync();
    }

    public async Task<InsurancePolicy?> GetByIdAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        return await context.Policies
            .AsNoTracking()
            .FirstOrDefaultAsync(p => p.Id == id);
    }

    public async Task<PolicyResult> AddAsync(InsurancePolicy policy)
    {
        var number = policy.PolicyNumber.Trim();
        var customerName = policy.CustomerName.Trim();
        var type = policy.PolicyType.Trim();

        if (!PolicyTypes.All.Contains(type))
        {
            return PolicyResult.InvalidPolicyType;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        if (await NumberExistsAsync(context, number, excludingId: null))
        {
            return PolicyResult.DuplicatePolicyNumber;
        }

        var newPolicy = new InsurancePolicy
        {
            PolicyNumber = number,
            CustomerName = customerName,
            PolicyType = type,
            MonthlyPremium = policy.MonthlyPremium,
            Status = PolicyStatuses.Pending
        };

        context.Policies.Add(newPolicy);

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Another request may have inserted the same number between our
            // check and the insert; the unique index catches that case.
            context.ChangeTracker.Clear();

            if (await NumberExistsAsync(context, number, excludingId: null))
            {
                return PolicyResult.DuplicatePolicyNumber;
            }

            throw;
        }

        policy.Id = newPolicy.Id;
        return PolicyResult.Success;
    }

    public async Task<PolicyResult> UpdateAsync(InsurancePolicy updatedPolicy)
    {
        var number = updatedPolicy.PolicyNumber.Trim();
        var customerName = updatedPolicy.CustomerName.Trim();
        var type = updatedPolicy.PolicyType.Trim();

        if (!PolicyTypes.All.Contains(type))
        {
            return PolicyResult.InvalidPolicyType;
        }

        if (!PolicyStatuses.All.Contains(updatedPolicy.Status))
        {
            return PolicyResult.InvalidStatus;
        }

        await using var context = await _contextFactory.CreateDbContextAsync();

        var existingPolicy = await context.Policies
            .FirstOrDefaultAsync(p => p.Id == updatedPolicy.Id);

        if (existingPolicy is null)
        {
            return PolicyResult.NotFound;
        }

        if (await NumberExistsAsync(context, number, excludingId: updatedPolicy.Id))
        {
            return PolicyResult.DuplicatePolicyNumber;
        }

        existingPolicy.PolicyNumber = number;
        existingPolicy.CustomerName = customerName;
        existingPolicy.PolicyType = type;
        existingPolicy.MonthlyPremium = updatedPolicy.MonthlyPremium;
        existingPolicy.Status = updatedPolicy.Status;

        try
        {
            await context.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            context.ChangeTracker.Clear();

            if (await NumberExistsAsync(context, number, excludingId: updatedPolicy.Id))
            {
                return PolicyResult.DuplicatePolicyNumber;
            }

            throw;
        }

        return PolicyResult.Success;
    }

    public async Task<bool> DeleteAsync(int id)
    {
        await using var context = await _contextFactory.CreateDbContextAsync();

        var policy = await context.Policies
            .FirstOrDefaultAsync(p => p.Id == id);

        if (policy is null)
        {
            return false;
        }

        context.Policies.Remove(policy);
        await context.SaveChangesAsync();

        return true;
    }

    private static Task<bool> NumberExistsAsync(
        PolicyFlowDbContext context, string policyNumber, int? excludingId)
    {
        var lowered = policyNumber.ToLower();

        return context.Policies.AnyAsync(p =>
            (excludingId == null || p.Id != excludingId) &&
            p.PolicyNumber.ToLower() == lowered);
    }
}
