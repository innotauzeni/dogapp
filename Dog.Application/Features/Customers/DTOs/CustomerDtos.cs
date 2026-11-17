namespace Dog.Application.Features.Customers.DTOs
{
    public class CustomerDto
    {
        public int Id { get; set; }
        public int CompanyId { get; set; }
        public string? Profile { get; set; }
        public string? Gender { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Dob { get; set; }
        public string? IDnumber { get; set; }
        public string? Cellphone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? JobTitle { get; set; }
        public int? CustomerCompanyId { get; set; }
        public string? CustomerCompanyName { get; set; }
    }

    public class CreateCustomerDto
    {
        public int CompanyId { get; set; }
        public string? Profile { get; set; }
        public string? Gender { get; set; }
        public string? FirstName { get; set; }
        public string? LastName { get; set; }
        public string? FullName { get; set; }
        public string? Dob { get; set; }
        public string? IDnumber { get; set; }
        public string? Cellphone { get; set; }
        public string? Email { get; set; }
        public string? Address { get; set; }
        public string? JobTitle { get; set; }
        public int? CustomerCompanyId { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateCustomerDto: CreateCustomerDto
    {
        public int Id { get; set; }
    }
}
