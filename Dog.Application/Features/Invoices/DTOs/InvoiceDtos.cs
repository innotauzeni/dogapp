namespace Dog.Application.Features.Invoices.DTOs
{
    public class InvoiceDto
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public int CompanyId { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal SubTotal { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string Status { get; set; } = "DRAFT";
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public DateTime CreatedAt { get; set; }
        public List<InvoiceItemDto> Items { get; set; } = new();
    }

    public class InvoiceItemDto
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal Amount { get; set; }
    }

    public class CreateInvoiceDto
    {
        public int CompanyId { get; set; }
        public string? CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
        public DateTime DueDate { get; set; }
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public List<CreateInvoiceItemDto> Items { get; set; } = new();
    }

    public class CreateInvoiceItemDto
    {
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountRate { get; set; }
    }

    public class UpdateInvoiceDto : CreateInvoiceDto
    {
        public int Id { get; set; }
    }

    // Payment recording DTO (cash/ecocash/bank)
    public class RecordPaymentDto
    {
        public int InvoiceId { get; set; }
        public int CompanyId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "CASH"; // CASH, ECOCASH, BANK
        public string? ReferenceNumber { get; set; }
        public string? TransactionId { get; set; }
        public string? Notes { get; set; }
        public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    }

    // Bank payment with Proof of Payment upload
    public class SubmitBankPaymentDto
    {
        public int InvoiceId { get; set; }
        public int CompanyId { get; set; }
        public int CustomerId { get; set; }
        public int BankId { get; set; }
        public int CurrencyId { get; set; }
        public decimal Amount { get; set; }
        public string? AccountNumber { get; set; }
        public string? StatementReference { get; set; }
        public string? Description { get; set; }
        public DateTime TransactionDate { get; set; } = DateTime.UtcNow;
        // Proof of Payment — base64 encoded file content
        public string? PopFileBase64 { get; set; }
        public string? PopFileName { get; set; }
    }

    // Proof of Payment DTO
    public class ProofOfPaymentDto
    {
        public int Id { get; set; }
        public int InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public string? FilePath { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public int? BankTransactionId { get; set; }
    }

    // Bank claim DTO — finance claims a bank transaction
    public class ClaimBankTransactionDto
    {
        public int BankTransactionId { get; set; }
        public int InvoiceId { get; set; }
        public int CompanyId { get; set; }
        public int CustomerId { get; set; }
        public int CurrencyId { get; set; }
        public string? Notes { get; set; }
    }

    // Apply suspense to invoice → generate receipt
    public class ApplySuspenseDto
    {
        public int SuspenseId { get; set; }
        public int InvoiceId { get; set; }
        public int CompanyId { get; set; }
        public int ExchangeRateId { get; set; }
        public decimal Amount { get; set; }
    }

    // Receipt DTO
    public class ReceiptDto
    {
        public int Id { get; set; }
        public string? ReceiptNumber { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int InvoiceId { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // Suspense DTO
    public class SuspenseDto
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int CurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
        public int SourceId { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public int CompanyId { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
