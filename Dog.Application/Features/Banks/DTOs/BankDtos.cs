namespace Dog.Application.Features.Banks.DTOs
{
    public class BankDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public string? AccountName { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; }
    }

    public class CreateBankDto
    {
        public string? Name { get; set; }
        public string? AccountName { get; set; }
        public int CompanyId { get; set; }
        public bool IsActive { get; set; }=true;
    }

    public class UpdateBankDto: CreateBankDto
    {
        public int Id { get; set; }
    }
}
