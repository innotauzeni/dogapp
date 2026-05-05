namespace Dog.Application.Features.BankAccounts.DTOs
{
    public class BankAccountDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int BankId { get; set; }
        public int CurrencyId { get; set; }
        public int CompanyId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchCode { get; set; }
        public string? SwiftCode { get; set; }
        public string? AccountNumber { get; set; }
        public bool IsActive { get; set; }
    }

    public class BankAccountListDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int BankId { get; set; }
        public string? BankName { get; set; }
        public int CurrencyId { get; set; }
        public string? CurrencyName { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public string? BranchName { get; set; }
        public string? BranchCode { get; set; }
        public string? SwiftCode { get; set; }
        public string? AccountNumber { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateBankAccountDto
    {
         public string? Name { get; set; }
        public int BankId { get; set; }
        public int CurrencyId { get; set; }
        public int CompanyId { get; set; }
        public string? BranchName { get; set; }
        public string? BranchCode { get; set; }
        public string? SwiftCode { get; set; }
        public string? AccountNumber { get; set; }
        public bool IsActive { get; set; } = true;
    }

    public class UpdateBankAccountDto: CreateBankAccountDto
    {
        public int Id { get; set; }
    }
}
