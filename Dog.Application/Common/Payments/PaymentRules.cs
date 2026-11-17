namespace Dog.Application.Common.Payments
{
    /// <summary>What the person recording a payment typed in (shared by company invoices and training invoices).</summary>
    public class PaymentInput
    {
        public string Method { get; set; } = "CASH";
        public decimal Amount { get; set; }
        public string CurrencyCode { get; set; } = "USD";
        public DateTime? PaymentDate { get; set; }
        /// <summary>The date the money actually moved (bank transfer date, mobile-money date, cheque date).</summary>
        public DateTime? ValueDate { get; set; }
        public string? PayerName { get; set; }
        public string? PayerContact { get; set; }
        public string? PayerBank { get; set; }
        public string? PayerAccount { get; set; }
        public int? DestinationBankAccountId { get; set; }
        public string? ReferenceNumber { get; set; }
        public string? ChequeNumber { get; set; }
        public string? ProofUrl { get; set; }
        public string? Notes { get; set; }
        /// <summary>The recorder says it is already on the bank statement. Only honoured for users allowed to verify payments.</summary>
        public bool VerifiedOnStatement { get; set; }
        public string? StatementReference { get; set; }
    }

    /// <summary>One way of paying, and exactly what has to be captured so the money can be traced.</summary>
    public record PaymentMethodInfo(string Code, string Label, string Icon, bool AutoConfirm, string[] Required, string Hint);

    public static class PaymentRules
    {
        public const string Pending = "PENDING";
        public const string Confirmed = "CONFIRMED";
        public const string Rejected = "REJECTED";

        // Required field keys: payerName, payerContact, payerBank, payerAccount, destinationAccount, reference, cheque, valueDate, notes
        public static readonly PaymentMethodInfo[] Methods =
        {
            new("CASH", "Cash", "bi-cash-stack", true, new[] { "payerName" },
                "Who handed over the cash. Optional: the receipt-book number as the reference."),
            new("ECOCASH", "EcoCash", "bi-phone", true, new[] { "payerName", "payerContact", "reference" },
                "The payer's EcoCash number and the transaction code from the confirmation SMS."),
            new("ONEMONEY", "OneMoney", "bi-phone", true, new[] { "payerName", "payerContact", "reference" },
                "The payer's OneMoney number and the transaction code from the confirmation SMS."),
            new("INNBUCKS", "InnBucks", "bi-phone", true, new[] { "payerName", "payerContact", "reference" },
                "The payer's InnBucks number and the transaction reference."),
            new("BANK", "Bank transfer", "bi-bank", false, new[] { "payerName", "payerBank", "reference", "valueDate", "destinationAccount" },
                "Who paid, from which bank, the bank's reference, the date, and which of OUR accounts received it. It stays PENDING until it is confirmed on the bank statement."),
            new("CARD", "Card / POS", "bi-credit-card-2-front", true, new[] { "reference" },
                "The POS slip / approval number. Optional: last 4 digits of the card as the payer account."),
            new("CHEQUE", "Cheque", "bi-journal-check", false, new[] { "payerName", "payerBank", "cheque", "valueDate" },
                "Cheque number, the bank it is drawn on and the cheque date. It stays PENDING until the cheque has cleared."),
            new("OTHER", "Other", "bi-three-dots", false, new[] { "reference", "notes" },
                "Describe how it was paid and give a reference. It stays PENDING until verified."),
        };

        public static PaymentMethodInfo? Find(string? code)
            => Methods.FirstOrDefault(m => string.Equals(m.Code, code?.Trim(), StringComparison.OrdinalIgnoreCase));

        /// <summary>Returns the first problem with the input, or null when everything required is present.</summary>
        public static string? Validate(PaymentInput p)
        {
            var info = Find(p.Method);
            if (info == null) return "Choose how the payment was made.";
            if (p.Amount <= 0) return "Enter an amount greater than zero.";
            if (p.ValueDate.HasValue && p.ValueDate.Value.Date > DateTime.Today.AddDays(1))
                return "The payment date cannot be in the future.";

            string Need(string label) => $"{label} is required for {info.Label} payments.";
            bool Blank(string? s) => string.IsNullOrWhiteSpace(s);
            foreach (var field in info.Required)
            {
                switch (field)
                {
                    case "payerName" when Blank(p.PayerName): return Need("The payer's name");
                    case "payerContact" when Blank(p.PayerContact): return Need("The payer's mobile number");
                    case "payerBank" when Blank(p.PayerBank): return Need("The payer's bank");
                    case "payerAccount" when Blank(p.PayerAccount): return Need("The payer's account number");
                    case "destinationAccount" when p.DestinationBankAccountId is null or <= 0: return Need("The bank account that received the money");
                    case "reference" when Blank(p.ReferenceNumber): return Need(info.Code is "BANK" ? "The bank reference" : "The transaction / slip reference");
                    case "cheque" when Blank(p.ChequeNumber): return Need("The cheque number");
                    case "valueDate" when p.ValueDate == null: return Need("The payment date");
                    case "notes" when Blank(p.Notes): return Need("A note describing the payment");
                }
            }
            if (p.VerifiedOnStatement && Blank(p.StatementReference))
                return "Enter the bank-statement line / reference you matched this payment to.";
            return null;
        }

        /// <summary>CONFIRMED straight away for cash / mobile / card; bank transfers and cheques wait for verification.</summary>
        public static string InitialStatus(PaymentMethodInfo info, bool canVerify, bool verifiedOnStatement)
            => info.AutoConfirm || (canVerify && verifiedOnStatement) ? Confirmed : Pending;

        public static string Trim(string? s) => (s ?? string.Empty).Trim();
    }
}
