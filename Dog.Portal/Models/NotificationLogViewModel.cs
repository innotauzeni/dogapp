namespace Dog.Portal.Models
{
    public class NotificationLogListViewModel
    {
        public List<NotificationLogItemViewModel> LogList { get; set; } = new();
        public string? TypeFilter { get; set; }
        public int Page { get; set; } = 1;
        public int PageSize { get; set; } = 20;
        public int TotalCount { get; set; }
        public int TotalPages => (int)Math.Ceiling((double)TotalCount / PageSize);
        public int From => TotalCount == 0 ? 0 : (Page - 1) * PageSize + 1;
        public int To => Math.Min(Page * PageSize, TotalCount);
    }

    public class NotificationLogItemViewModel
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
