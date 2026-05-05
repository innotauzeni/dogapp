using Dog.Application.Features.Banks.DTOs;
using Dog.Application.Features.BankAccounts.DTOs;

namespace Dog.Portal.Models
{
    // ── Bank ViewModels ──────────────────────────────────────────────

    public class BankListViewModel
    {
        public List<BankDto> BankList { get; set; } = new();
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

    public class CreateBankRequest
    {
        public string? Name { get; set; }
        public string? AccountName { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateBankRequest : CreateBankRequest
    {
        public int Id { get; set; }
    }

    // ── Bank Account ViewModels ──────────────────────────────────────────────

    public class BankAccountListViewModel
    {
        public List<BankAccountListDto> BankAccountList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int? BankId { get; set; }
        public string? BankName { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int From => (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
        public bool HasPrevious => Page > 1;
        public bool HasNext => Page < TotalPages;
        
        // For dropdowns
        public List<BankDto> Banks { get; set; } = new();
        public List<CurrencyDto> Currencies { get; set; } = new();
    }

    public class CreateBankAccountRequest
    {
        public string? Name { get; set; }
        public int BankId { get; set; }
        public int CurrencyId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchCode { get; set; }
        public string? SwiftCode { get; set; }
        public string? AccountNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateBankAccountRequest : CreateBankAccountRequest
    {
        public int Id { get; set; }
    }
}