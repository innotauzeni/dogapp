namespace Dog.Application.Features.PaymentMethods.DTOs
{
    public class PaymentMethodDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public bool IsActive { get; set; }
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class CreatePaymentMethodDto
    {
        public string Name { get; set; } = string.Empty;
        public bool IsActive { get; set; } = true;
        public int CompanyId { get; set; }
    }

    public class UpdatePaymentMethodDto : CreatePaymentMethodDto
    {
        public int Id { get; set; }
    }
}
