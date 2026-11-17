namespace Dog.Application.Features.Notifications.DTOs
{
    public class NotificationLogDto
    {
        public int Id { get; set; }
        public string NotificationType { get; set; } = string.Empty;
        public string? RecipientPhone { get; set; }
        public string Message { get; set; } = string.Empty;
        public DateTime SentAt { get; set; }
        public bool Success { get; set; }
        public string? RelatedEntityType { get; set; }
        public int? RelatedEntityId { get; set; }
    }
}
