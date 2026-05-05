namespace Dog.Portal.Models
{
    // ── Invoice list view model ───────────────────────────────────────────────
    public class InvoiceListViewModel
    {
        public List<InvoiceItemViewModel> InvoiceList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public string? StatusFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);

        // Stats
        public int DraftCount { get; set; }
        public int SentCount { get; set; }
        public int PartialCount { get; set; }
        public int PaidCount { get; set; }
        public decimal TotalOutstanding { get; set; }
        public decimal TotalPaid { get; set; }

        // Dropdowns
        public List<CustomerDropdown> Customers { get; set; } = new();
        public List<SuspenseDropdown> PendingSuspense { get; set; } = new();
        public List<BankTxDropdown> UnclaimedBankTx { get; set; } = new();
        public List<CurrencyDropdown> Currencies { get; set; } = new();
        public List<BankDropdown> Banks { get; set; } = new();
        public List<BankAccountDropdown> BankAccounts { get; set; } = new();
    }

    public class InvoiceItemViewModel
    {
        public int Id { get; set; }
        public string InvoiceNumber { get; set; } = string.Empty;
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public DateTime InvoiceDate { get; set; }
        public DateTime DueDate { get; set; }
        public decimal TotalAmount { get; set; }
        public decimal PaidAmount { get; set; }
        public decimal BalanceAmount { get; set; }
        public string Status { get; set; } = "DRAFT";
        public string? Notes { get; set; }
        public List<InvoiceLineItem> Items { get; set; } = new();
    }

    public class InvoiceLineItem
    {
        public int Id { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountRate { get; set; }
        public decimal Amount { get; set; }
    }

    // ── Suspense list view model ──────────────────────────────────────────────
    public class SuspenseListViewModel
    {
        public List<SuspenseItemViewModel> SuspenseList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
        public List<InvoiceDropdown> Invoices { get; set; } = new();
        public List<CurrencyDropdown> Currencies { get; set; } = new();
    }

    public class SuspenseItemViewModel
    {
        public int Id { get; set; }
        public int CustomerId { get; set; }
        public string? CustomerName { get; set; }
        public int CurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
        public int SourceId { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Receipt list view model ───────────────────────────────────────────────
    public class ReceiptListViewModel
    {
        public List<ReceiptItemViewModel> ReceiptList { get; set; } = new();
        public string? SearchTerm { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 15;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    public class ReceiptItemViewModel
    {
        public int Id { get; set; }
        public string? ReceiptNumber { get; set; }
        public string? CustomerName { get; set; }
        public string? InvoiceNumber { get; set; }
        public decimal Amount { get; set; }
        public string? Status { get; set; }
        public string? CurrencyCode { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Dropdown helpers ──────────────────────────────────────────────────────
    public class CustomerDropdown { public int Id { get; set; } public string? FullName { get; set; } public string? Email { get; set; } }
    public class SuspenseDropdown { public int Id { get; set; } public string? CustomerName { get; set; } public decimal Amount { get; set; } public string? CurrencyCode { get; set; } }
    public class BankTxDropdown { public int Id { get; set; } public string? Name { get; set; } public string? Amount { get; set; } public string? BankName { get; set; } public string? CustomerName { get; set; } }
    public class InvoiceDropdown { public int Id { get; set; } public string? InvoiceNumber { get; set; } public decimal BalanceAmount { get; set; } public string? CustomerName { get; set; } }
    public class CurrencyDropdown { public int Id { get; set; } public string? Code { get; set; } public string? Name { get; set; } }
    public class BankDropdown { public int Id { get; set; } public string? Name { get; set; } public string? AccountName { get; set; } }
    public class BankAccountDropdown
    {
        public int Id { get; set; }
        public int BankId { get; set; }
        public string? Name { get; set; }
        public string? AccountNumber { get; set; }
        public int CurrencyId { get; set; }
        public string? CurrencyCode { get; set; }
        public string? CurrencyName { get; set; }
        public string? BranchName { get; set; }
        public string? SwiftCode { get; set; }
    }

    // ── Request models ────────────────────────────────────────────────────────
    public class CreateInvoiceRequest
    {
        public string? CustomerName { get; set; }
        public string? CustomerEmail { get; set; }
        public string? CustomerPhone { get; set; }
        public string? CustomerId { get; set; }
        public string InvoiceDate { get; set; } = string.Empty;
        public string DueDate { get; set; } = string.Empty;
        public decimal TaxAmount { get; set; }
        public decimal DiscountAmount { get; set; }
        public string? Notes { get; set; }
        public string? Terms { get; set; }
        public List<InvoiceLineRequest> Items { get; set; } = new();
    }

    public class InvoiceLineRequest
    {
        public string Description { get; set; } = string.Empty;
        public int Quantity { get; set; } = 1;
        public decimal UnitPrice { get; set; }
        public decimal TaxRate { get; set; }
        public decimal DiscountRate { get; set; }
    }

    public class RecordPaymentRequest
    {
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public string PaymentMethod { get; set; } = "CASH";
        public string? ReferenceNumber { get; set; }
        public string? Notes { get; set; }
    }

    public class SubmitBankPaymentRequest
    {
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public int BankId { get; set; }
        public int CurrencyId { get; set; }
        public decimal Amount { get; set; }
        public string? AccountNumber { get; set; }
        public string? StatementReference { get; set; }
        public string? Description { get; set; }
        // POP file as base64
        public string? PopFileBase64 { get; set; }
        public string? PopFileName { get; set; }
    }

    public class ClaimBankTxRequest
    {
        public int BankTransactionId { get; set; }
        public int InvoiceId { get; set; }
        public int CustomerId { get; set; }
        public int CurrencyId { get; set; }
    }

    public class ApplySuspenseRequest
    {
        public int SuspenseId { get; set; }
        public int InvoiceId { get; set; }
        public decimal Amount { get; set; }
        public int ExchangeRateId { get; set; } = 1;
    }
}
