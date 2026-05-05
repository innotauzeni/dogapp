namespace Dog.Portal.Models
{
    public class DashboardEvent
    {
        public string Id { get; set; } = Guid.NewGuid().ToString();
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public DateTime EventDate { get; set; }
        public string? EventTime { get; set; }
        public string Category { get; set; } = "general"; // general, vaccination, mating, litter, deployment, other
        public string Color { get; set; } = "#667eea";
        public bool IsCompleted { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }

    public class CreateEventRequest
    {
        public string Title { get; set; } = string.Empty;
        public string? Description { get; set; }
        public string EventDate { get; set; } = string.Empty;
        public string? EventTime { get; set; }
        public string Category { get; set; } = "general";
    }
}
