using Dog.Application.Features.Customers.DTOs;

namespace Dog.Portal.Models
{
    // ── Customer ViewModels ──────────────────────────────────────────────

    public class CustomerListViewModel
    {
        public List<CustomerDto> CustomerList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int From => (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
    }

    public class CreateCustomerRequest
    {
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
        public bool IsActive { get; set; } = true;
    }

    public class UpdateCustomerRequest : CreateCustomerRequest
    {
        public int Id { get; set; }
    }
}