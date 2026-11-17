using Dog.Core.Common;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities;

// The Dog Training module keeps its own customers, dogs, invoices and payments. The dogs sent for training belong
// to outside customers, so they are deliberately NOT the company's own dogs (ka_dogs) or customers (ka_customers),
// and training money is never mixed with the company's Invoices module.

/// <summary>A person or business that brings a dog for training.</summary>
public class DogTrainingCustomer : AuditableEntity
{
    public string FullName { get; set; } = string.Empty;
    public string? Email { get; set; }
    public string? Phone { get; set; }
    public string? Address { get; set; }
    public string? IdNumber { get; set; }
    public string? Notes { get; set; }

    public virtual ICollection<DogTrainingDog> Dogs { get; set; } = new List<DogTrainingDog>();
}

/// <summary>A customer's dog that is (or has been) enrolled for training.</summary>
public class DogTrainingDog : AuditableEntity
{
    public int CustomerId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Breed { get; set; }
    public string? Color { get; set; }
    public string? Gender { get; set; }
    public DateTime? DateOfBirth { get; set; }
    public string? MicrochipNumber { get; set; }
    public string? PhotoUrl { get; set; }
    public string? Notes { get; set; }

    public virtual DogTrainingCustomer Customer { get; set; } = null!;
}

/// <summary>Invoice for a training enrolment. Status: DRAFT / SENT / PARTIAL / PAID / CANCELLED.</summary>
public class DogTrainingInvoice : AuditableEntity
{
    public string InvoiceNumber { get; set; } = string.Empty;
    public int CustomerId { get; set; }
    public string? CustomerName { get; set; }
    public string? CustomerEmail { get; set; }
    public string? CustomerPhone { get; set; }
    public DateTime InvoiceDate { get; set; } = DateTime.UtcNow;
    public DateTime DueDate { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal TotalAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal PaidAmount { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal BalanceAmount { get; set; }
    public string Status { get; set; } = "SENT";
    public string? Notes { get; set; }
    public string? Terms { get; set; }
    public string? CreatedByName { get; set; }
    public DateTime? EmailedAt { get; set; }
    public string? EmailedTo { get; set; }

    public virtual DogTrainingCustomer Customer { get; set; } = null!;
    public virtual ICollection<DogTrainingInvoiceItem> Items { get; set; } = new List<DogTrainingInvoiceItem>();
    public virtual ICollection<DogTrainingPayment> Payments { get; set; } = new List<DogTrainingPayment>();
}

public class DogTrainingInvoiceItem : AuditableEntity
{
    public int InvoiceId { get; set; }
    public string Description { get; set; } = string.Empty;
    public int Quantity { get; set; } = 1;
    [Column(TypeName = "decimal(18,2)")] public decimal UnitPrice { get; set; }
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }

    public virtual DogTrainingInvoice Invoice { get; set; } = null!;
}

/// <summary>Money received against a training invoice.</summary>
public class DogTrainingPayment : AuditableEntity
{
    public int InvoiceId { get; set; }
    public string PaymentNumber { get; set; } = string.Empty;
    public DateTime PaymentDate { get; set; } = DateTime.UtcNow;
    [Column(TypeName = "decimal(18,2)")] public decimal Amount { get; set; }
    /// <summary>CASH / ECOCASH / BANK / CARD / OTHER.</summary>
    public string Method { get; set; } = "CASH";
    public string? ReferenceNumber { get; set; }
    public string? Notes { get; set; }
    public string? ReceivedByUserId { get; set; }
    public string? ReceivedByName { get; set; }

    // Trace (same structure as company invoice payments)
    public DateTime? ValueDate { get; set; }
    public string CurrencyCode { get; set; } = "USD";
    /// <summary>PENDING / CONFIRMED / REJECTED. Only CONFIRMED payments reduce the balance.</summary>
    public string Status { get; set; } = "CONFIRMED";
    public string? PayerName { get; set; }
    public string? PayerContact { get; set; }
    public string? PayerBank { get; set; }
    public string? PayerAccount { get; set; }
    public int? DestinationBankAccountId { get; set; }
    public string? ChequeNumber { get; set; }
    public string? ProofUrl { get; set; }
    public string? ConfirmedByUserId { get; set; }
    public string? ConfirmedByName { get; set; }
    public DateTime? ConfirmedAt { get; set; }
    public string? StatementReference { get; set; }
    public string? RejectReason { get; set; }

    public virtual DogTrainingInvoice Invoice { get; set; } = null!;
}
