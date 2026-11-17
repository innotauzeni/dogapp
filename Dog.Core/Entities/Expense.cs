using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>An operating cost (vet bill, food, fuel, salary, equipment...), optionally tied to a specific dog.</summary>
public class Expense : AuditableEntity
{
    public string Category { get; set; } = string.Empty; // Food, Vet, Fuel, Salaries, Equipment, Training, Other
    public string Description { get; set; } = string.Empty;
    public decimal Amount { get; set; }
    public DateTime ExpenseDate { get; set; }
    public int? DogId { get; set; }
    public string? Vendor { get; set; }
    public string? PaymentMethod { get; set; }
    public string? Notes { get; set; }

    public virtual Dogtbl? Dog { get; set; }
}
