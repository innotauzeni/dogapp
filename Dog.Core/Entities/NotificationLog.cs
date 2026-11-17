using Dog.Core.Common;

namespace Dog.Core.Entities;

/// <summary>A record of a reminder the system sent out (vaccine due, contract renewal, invoice overdue...).</summary>
public class NotificationLog : AuditableEntity
{
    public string NotificationType { get; set; } = string.Empty; // VaccineDue, ContractRenewal, InvoiceOverdue
    public string? RecipientPhone { get; set; }
    public string Message { get; set; } = string.Empty;
    public DateTime SentAt { get; set; } = DateTime.UtcNow;
    public bool Success { get; set; }
    public string? RelatedEntityType { get; set; }
    public int? RelatedEntityId { get; set; }
}
