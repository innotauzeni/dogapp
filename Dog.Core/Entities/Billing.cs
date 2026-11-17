using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities;

/// <summary>A client company that is billed. Customers (contact people) belong to a customer company and invoices are
/// addressed to the company, with its registration / tax / VAT details printed on the invoice.</summary>
public class CustomerCompany : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? TradingName { get; set; }
    public string? RegistrationNumber { get; set; }
    /// <summary>Tax / BP number.</summary>
    public string? TaxNumber { get; set; }
    public string? VatNumber { get; set; }
    public string? Email { get; set; }
    /// <summary>Where invoices are emailed (one or more addresses separated by ; or ,). Falls back to Email.</summary>
    public string? BillingEmail { get; set; }
    public string? Phone { get; set; }
    public string? Website { get; set; }
    public string? Address { get; set; }
    public string? City { get; set; }
    public string? Country { get; set; }
    public string? ContactPerson { get; set; }
    public int PaymentTermsDays { get; set; } = 30;
    public string? Notes { get; set; }

    public virtual ICollection<Customer> Contacts { get; set; } = new List<Customer>();
}

/// <summary>
/// Money received against a company invoice, with everything needed to trace it: how it was paid, who paid, from which
/// bank / number, the reference, which of OUR accounts received it, and who recorded and confirmed it.
/// Only CONFIRMED payments reduce the invoice balance (a bank transfer stays PENDING until seen on the statement).
/// </summary>
public class InvoicePayment : AuditableEntity
{
    public int InvoiceId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;          // RCP-2026-0001
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;       // when it was recorded / received
    public DateTime? ValueDate { get; set; }                           // date the money actually moved (bank / mobile)
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    /// <summary>CASH / ECOCASH / ONEMONEY / BANK / CARD / CHEQUE / OTHER.</summary>
    public string Method { get; set; } = "CASH";
    /// <summary>PENDING / CONFIRMED / REJECTED.</summary>
    public string Status { get; set; } = "CONFIRMED";

    // Trace
    public string? PayerName { get; set; }
    public string? PayerContact { get; set; }          // payer's mobile number / email
    public string? PayerBank { get; set; }
    public string? PayerAccount { get; set; }          // payer account no. / card last 4
    public int? DestinationBankAccountId { get; set; } // our bank account that received it
    public string? ReferenceNumber { get; set; }       // bank reference / EcoCash transaction code / POS slip no.
    public string? ChequeNumber { get; set; }
    public string? ProofUrl { get; set; }
    public string? Notes { get; set; }

    public string? ReceivedByUserId { get; set; }
    public string? ReceivedByName { get; set; }
    public string? ConfirmedByUserId { get; set; }
    public string? ConfirmedByName { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? StatementReference { get; set; }    // line on the bank statement it was matched to
    public string? RejectReason { get; set; }

    public virtual Invoice Invoice { get; set; } = null!;
}

/// <summary>Outgoing mail (SMTP) account for a company, e.g. billing@theirdomain.co.zw. The password is stored encrypted.</summary>
public class EmailSetting : AuditableEntity
{
    public string Host { get; set; } = string.Empty;
    public int Port { get; set; } = 587;
    /// <summary>None / SslOnConnect (465) / StartTls (587).</summary>
    public string Security { get; set; } = "StartTls";
    public string? Username { get; set; }
    public string? PasswordEncrypted { get; set; }
    public string FromAddress { get; set; } = string.Empty;
    public string? FromName { get; set; }
    public string? ReplyTo { get; set; }
    public string? BccAddress { get; set; }
    public string? Signature { get; set; }
    public DateTime? LastTestAt { get; set; }
    public bool? LastTestOk { get; set; }
    public string? LastTestMessage { get; set; }
}

/// <summary>One row per email the system tried to send (invoices, receipts, tests) so nothing is sent "invisibly".</summary>
public class EmailLog : AuditableEntity
{
    public string ToAddresses { get; set; } = string.Empty;
    public string? CcAddresses { get; set; }
    public string Subject { get; set; } = string.Empty;
    /// <summary>Invoice / TrainingInvoice / Receipt / Test …</summary>
    public string? RelatedType { get; set; }
    public int? RelatedId { get; set; }
    public bool Success { get; set; }
    public string? Error { get; set; }
    public string? SentByUserId { get; set; }
    public string? SentByName { get; set; }
    public bool HasAttachment { get; set; }
}

/// <summary>A saved service / product line the invoice builder can add in one click.</summary>
public class InvoiceCatalogItem : AuditableEntity
{
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(5,2)")] public decimal TaxRate { get; set; }
}
