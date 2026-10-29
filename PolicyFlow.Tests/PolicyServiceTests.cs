using PolicyFlow.Data;
using PolicyFlow.Models;
using PolicyFlow.Services;

namespace PolicyFlow.Tests;

public class PolicyServiceTests
{
    private readonly TestDbContextFactory _factory = new();
    private readonly PolicyService _service;

    public PolicyServiceTests()
    {
        _service = new PolicyService(_factory);
    }

    private static InsurancePolicy NewPolicy(
        string number = "POL-001",
        string name = "Jane Doe",
        string type = PolicyTypes.Vehicle,
        decimal premium = 500m) => new()
        {
            PolicyNumber = number,
            CustomerName = name,
            PolicyType = type,
            MonthlyPremium = premium
        };

    private async Task<int> AddAndGetIdAsync(InsurancePolicy policy)
    {
        var result = await _service.AddAsync(policy);
        Assert.Equal(PolicyResult.Success, result);
        return policy.Id;
    }

    [Fact]
    public async Task Add_ValidPolicy_IsSavedAsPending()
    {
        var policy = NewPolicy();
        policy.Status = PolicyStatuses.Active; // callers can't skip the Pending stage

        var id = await AddAndGetIdAsync(policy);

        var saved = await _service.GetByIdAsync(id);
        Assert.NotNull(saved);
        Assert.Equal(PolicyStatuses.Pending, saved!.Status);
    }

    [Fact]
    public async Task Add_TrimsWhitespaceFromInput()
    {
        var id = await AddAndGetIdAsync(NewPolicy("  POL-002  ", "  Jane Doe ", " Home "));

        var saved = await _service.GetByIdAsync(id);

        Assert.Equal("POL-002", saved!.PolicyNumber);
        Assert.Equal("Jane Doe", saved.CustomerName);
        Assert.Equal(PolicyTypes.Home, saved.PolicyType);
    }

    [Theory]
    [InlineData("POL-001")]
    [InlineData("pol-001")]
    [InlineData("  POL-001 ")]
    public async Task Add_DuplicateNumber_IsRejectedRegardlessOfCaseOrSpacing(string duplicate)
    {
        await AddAndGetIdAsync(NewPolicy("POL-001"));

        var result = await _service.AddAsync(NewPolicy(duplicate));

        Assert.Equal(PolicyResult.DuplicatePolicyNumber, result);
        Assert.Single(await _service.GetAllAsync());
    }

    [Fact]
    public async Task Add_InvalidType_IsRejected()
    {
        var result = await _service.AddAsync(NewPolicy(type: "Boat"));

        Assert.Equal(PolicyResult.InvalidPolicyType, result);
        Assert.Empty(await _service.GetAllAsync());
    }

    [Fact]
    public async Task Update_ChangesAllEditableFields()
    {
        var id = await AddAndGetIdAsync(NewPolicy());

        var result = await _service.UpdateAsync(new InsurancePolicy
        {
            Id = id,
            PolicyNumber = "POL-NEW",
            CustomerName = "John Smith",
            PolicyType = PolicyTypes.Life,
            MonthlyPremium = 900m,
            Status = PolicyStatuses.Active
        });

        Assert.Equal(PolicyResult.Success, result);

        var saved = await _service.GetByIdAsync(id);
        Assert.Equal("POL-NEW", saved!.PolicyNumber);
        Assert.Equal("John Smith", saved.CustomerName);
        Assert.Equal(PolicyTypes.Life, saved.PolicyType);
        Assert.Equal(900m, saved.MonthlyPremium);
        Assert.Equal(PolicyStatuses.Active, saved.Status);
    }

    [Fact]
    public async Task Update_KeepingOwnPolicyNumber_IsAllowed()
    {
        var id = await AddAndGetIdAsync(NewPolicy("POL-001"));

        var result = await _service.UpdateAsync(new InsurancePolicy
        {
            Id = id,
            PolicyNumber = "POL-001",
            CustomerName = "Renamed",
            PolicyType = PolicyTypes.Vehicle,
            MonthlyPremium = 500m,
            Status = PolicyStatuses.Pending
        });

        Assert.Equal(PolicyResult.Success, result);
    }

    [Fact]
    public async Task Update_NumberUsedByAnotherPolicy_IsRejected()
    {
        await AddAndGetIdAsync(NewPolicy("POL-001"));
        var secondId = await AddAndGetIdAsync(NewPolicy("POL-002"));

        var result = await _service.UpdateAsync(new InsurancePolicy
        {
            Id = secondId,
            PolicyNumber = "pol-001",
            CustomerName = "Jane Doe",
            PolicyType = PolicyTypes.Vehicle,
            MonthlyPremium = 500m,
            Status = PolicyStatuses.Pending
        });

        Assert.Equal(PolicyResult.DuplicatePolicyNumber, result);
        Assert.Equal("POL-002", (await _service.GetByIdAsync(secondId))!.PolicyNumber);
    }

    [Fact]
    public async Task Update_MissingPolicy_ReturnsNotFound()
    {
        var policy = NewPolicy();
        policy.Id = 999;

        Assert.Equal(PolicyResult.NotFound, await _service.UpdateAsync(policy));
    }

    [Fact]
    public async Task Update_InvalidStatus_IsRejected()
    {
        var id = await AddAndGetIdAsync(NewPolicy());

        var result = await _service.UpdateAsync(new InsurancePolicy
        {
            Id = id,
            PolicyNumber = "POL-001",
            CustomerName = "Jane Doe",
            PolicyType = PolicyTypes.Vehicle,
            MonthlyPremium = 500m,
            Status = "Expired"
        });

        Assert.Equal(PolicyResult.InvalidStatus, result);
    }

    [Fact]
    public async Task Update_DoesNotModifyTheObjectPassedIn()
    {
        var id = await AddAndGetIdAsync(NewPolicy());
        var edited = new InsurancePolicy
        {
            Id = id,
            PolicyNumber = "  POL-001  ",
            CustomerName = "Jane Doe",
            PolicyType = PolicyTypes.Vehicle,
            MonthlyPremium = 500m,
            Status = PolicyStatuses.Pending
        };

        await _service.UpdateAsync(edited);

        Assert.Equal("  POL-001  ", edited.PolicyNumber);
    }

    [Fact]
    public async Task Delete_ExistingPolicy_RemovesIt()
    {
        var id = await AddAndGetIdAsync(NewPolicy());

        Assert.True(await _service.DeleteAsync(id));
        Assert.Null(await _service.GetByIdAsync(id));
    }

    [Fact]
    public async Task Delete_MissingPolicy_ReturnsFalse()
    {
        Assert.False(await _service.DeleteAsync(999));
    }

    [Fact]
    public async Task GetAll_ReturnsPoliciesOrderedById()
    {
        await AddAndGetIdAsync(NewPolicy("POL-B"));
        await AddAndGetIdAsync(NewPolicy("POL-A"));

        var all = await _service.GetAllAsync();

        Assert.Equal(new[] { "POL-B", "POL-A" }, all.Select(p => p.PolicyNumber));
    }

    [Fact]
    public async Task Seeder_InsertsSampleDataOnlyOnce()
    {
        await using (var context = _factory.CreateDbContext())
        {
            await DbSeeder.SeedAsync(context);
            await DbSeeder.SeedAsync(context);
        }

        var all = await _service.GetAllAsync();

        Assert.NotEmpty(all);
        Assert.Equal(all.Count, all.Select(p => p.PolicyNumber).Distinct().Count());
    }

    [Fact]
    public void FailedResults_HaveUserFriendlyMessages()
    {
        foreach (var result in Enum.GetValues<PolicyResult>().Where(r => r != PolicyResult.Success))
        {
            Assert.False(string.IsNullOrWhiteSpace(result.ToMessage()));
        }
    }
}
