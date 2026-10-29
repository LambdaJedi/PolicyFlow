using System.ComponentModel.DataAnnotations;

namespace PolicyFlow.Models;

public class InsurancePolicy
{
    public int Id { get; set; }

    [Required]
    [StringLength(50)]
    public string PolicyNumber { get; set; } = "";

    [Required]
    public string CustomerName { get; set; } = "";

    [Required]
    public string PolicyType { get; set; } = "";

    [Range(1, 1000000)]
    public decimal MonthlyPremium { get; set; }

    public string Status { get; set; } = PolicyStatuses.Pending;
}
