
using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;
using System.Diagnostics.Metrics;

namespace Dog.Core.Entities
{
    public class Company : AuditableBaseEntity
    {
        public string? CompanyName { get; set; }
        public string? CompanyCode { get; set; }
        public DateTime? Establish { get; set; }
        public string? Location { get; set; }
        public string? FolderSlug { get; set; }
        public string? PhoneNumber { get; set; }
        public string? EmailAddress { get; set; }
        public string? Website { get; set; }
        public int CountryId { get; set; }
        public int ProvinceId { get; set; }
        public int CityId { get; set; }
        public string? Address { get; set; }
        public string? Logo { get; set; }
        /// Hex color (e.g. "#1D4ED8") used to brand the mobile app's welcome screen for this company.
        public string? BrandColor { get; set; }

        // Printed on invoices / used for fiscal-style invoices
        public string? RegistrationNumber { get; set; }
        public string? TaxNumber { get; set; }
        public string? VatNumber { get; set; }
        /// <summary>Default invoice terms and footer text (e.g. payment instructions, thank-you line).</summary>
        public string? InvoiceTerms { get; set; }
        public string? InvoiceFooter { get; set; }
        [Column(TypeName = "decimal(5,2)")] public decimal DefaultVatRate { get; set; }
        public bool FiscalEnabled { get; set; }
        public string? FiscalDeviceSerial { get; set; }
        public string? UserId { get; set; }
        //public bool IsActive { get; set; } = false;
        public long TotalStorageUsedBytes { get; set; } = 0;

        public int DefaultCurrencyId { get; set; }
        public Currency DefaultCurrency { get; set; } = null!;

        [ForeignKey(nameof(CountryId))]
        public Country? Country { get; set; }

        [ForeignKey(nameof(ProvinceId))]
        public Province? Province { get; set; }

        [ForeignKey(nameof(CityId))]
        public City? City { get; set; }

        // Self-referencing parent/branch
        public int? ParentCompanyId { get; set; }  // null if head office
        [ForeignKey(nameof(ParentCompanyId))]
        public Company? ParentCompany { get; set; }

        public ICollection<Company> Branches { get; set; } = new List<Company>();

        // --- Helper property to identify type ---
        [NotMapped]
        public string CompanyType => ParentCompanyId == null ? "Head Office" : "Branch";
    }
}
