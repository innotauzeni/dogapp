namespace Dog.Portal.Models
{
    public class ExpenseListViewModel
    {
        public List<ExpenseItemViewModel> ExpenseList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? CategoryFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);

        public decimal TotalAmount { get; set; }
        public decimal ThisMonthAmount { get; set; }

        public List<AnimalListDto> Dogs { get; set; } = new();
    }

    public class ExpenseItemViewModel
    {
        public int Id { get; set; }
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

    public class CreateExpenseRequest
    {
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string ExpenseDate { get; set; } = string.Empty;
        public int? DogId { get; set; }
        public string? Vendor { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
    }

    public class UpdateExpenseRequest : CreateExpenseRequest
    {
        public int Id { get; set; }
    }
}
