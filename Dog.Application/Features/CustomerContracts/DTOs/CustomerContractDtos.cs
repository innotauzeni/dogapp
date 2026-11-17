namespace Dog.Application.Features.CustomerContracts.DTOs
{
    public class CustomerContractDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public DateTime ContractDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? ContractDetails { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateCustomerContractDto
    {
        public int CustomerId { get; set; }
        public int CompanyId { get; set; }
        public DateTime ContractDate { get; set; }
        public DateTime? ExpiryDate { get; set; }
        public string? ContractDetails { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateCustomerContractDto : CreateCustomerContractDto
    {
        public int Id { get; set; }
    }
}
