

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class Customer : AuditableEntity
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
        public string? JobTitle { get; set; }
        /// <summary>The company this contact person works for (invoices are billed to the company).</summary>
        public int? CustomerCompanyId { get; set; }
        public virtual CustomerCompany? CustomerCompany { get; set; }
        //public bool IsActive { get; set; } = true;

        public virtual ICollection<CustomerSite> Sites { get; set; } = new List<CustomerSite>();
        public virtual ICollection<CustomerContract> Contracts { get; set; } = new List<CustomerContract>();
    }

}
