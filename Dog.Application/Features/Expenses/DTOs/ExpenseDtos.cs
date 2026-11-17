namespace Dog.Application.Features.Expenses.DTOs
{
    public class ExpenseDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; }
        public int? DogId { get; set; }
        public string? DogName { get; set; }
        public string? Vendor { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }

    public class CreateExpenseDto
    {
        public int CompanyId { get; set; }
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public DateTime ExpenseDate { get; set; } = DateTime.UtcNow;
        public int? DogId { get; set; }
        public string? Vendor { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateExpenseDto : CreateExpenseDto
    {
        public int Id { get; set; }
    }
}
