namespace Dog.Application.Features.BankTransactions.DTOs
{
    public class BankTransactionDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int BankId { get; set; }
        public int CustomerId { get; set; }
        public int CurrencyId { get; set; }
        public int CompanyId { get; set; }
        public int ProofofPaymentId { get; set; }
        public string? StatementReference { get; set; }
        public string? SourceReference { get; set; }
        public string? Description { get; set; }
        public string? Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? Status { get; set; }
    }

    public class BankTransactionListDto
    {
        public int Id { get; set; }
        public string? Name { get; set; }
        public int BankId { get; set; }
        public string? BankName { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int CurrencyId { get; set; }
        public string? CurrencyName { get; set; }
        public int CompanyId { get; set; }
        public string? CompanyName { get; set; }
        public int ProofofPaymentId { get; set; }
        public string? ProofofPaymentName { get; set; }
        public string? StatementReference { get; set; }
        public string? SourceReference { get; set; }
        public string? Description { get; set; }
        public string? Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? Status { get; set; }
    }


    public class CreateBankTransactionDto
    {
        public string? Name { get; set; }
        public int BankId { get; set; }
        public int CustomerId { get; set; }
        public int CurrencyId { get; set; }
        public int CompanyId { get; set; }
        public int ProofofPaymentId { get; set; }
        public string? StatementReference { get; set; }
        public string? SourceReference { get; set; }
        public string? Description { get; set; }
        public string? Amount { get; set; }
        public DateTime? TransactionDate { get; set; }
        public string? AccountNumber { get; set; }
        public string? Status { get; set; }
    }

    public class UpdateBankTransactionDto: CreateBankTransactionDto
    {
        public int Id { get; set; }
    }





}
