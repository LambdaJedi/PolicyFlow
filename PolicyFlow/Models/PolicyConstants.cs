namespace PolicyFlow.Models;

/// <summary>Allowed values for <see cref="InsurancePolicy.Status"/>.</summary>
public static class PolicyStatuses
{
    public const string Pending = "Pending";
    public const string Active = "Active";
    public const string Cancelled = "Cancelled";

    public static readonly IReadOnlyList<string> All = new[] { Pending, Active, Cancelled };
}

/// <summary>Allowed values for <see cref="InsurancePolicy.PolicyType"/>.</summary>
public static class PolicyTypes
{
    public const string Life = "Life";
    public const string Vehicle = "Vehicle";
    public const string Home = "Home";

    public static readonly IReadOnlyList<string> All = new[] { Life, Vehicle, Home };

    public static string DisplayName(string type) => $"{type} Insurance";
}
