namespace Dog.Portal.Models
{

    public class PaymentMethodDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentMethodRequest
    {
        public string Name { get; set; } = string.Empty;
        public int CompanyId { get; set; }
    }

    public class UpdatePaymentMethodRequest : CreatePaymentMethodRequest
    {
        public int Id { get; set; }
    }
    public class PaymentMethodsListViewModel
    {
        public List<PaymentMethodDto> PaymentMethods { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => PageSize > 0 ? (int)Math.Ceiling((double)TotalCount / PageSize) : 1;
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }
}
