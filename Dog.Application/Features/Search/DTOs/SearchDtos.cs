namespace Dog.Application.Features.Search.DTOs
{
    public class SearchResultItemDto
    {
        public string Type { get; set; } = string.Empty; // "Dog", "Customer", "Invoice"
        public int Id { get; set; }
        public string Title { get; set; } = string.Empty;
        public string? Subtitle { get; set; }
    }

    public class SearchResultsDto
    {
        public List<SearchResultItemDto> Dogs { get; set; } = new();
        public List<SearchResultItemDto> Customers { get; set; } = new();
        public List<SearchResultItemDto> Invoices { get; set; } = new();
        public int TotalCount => Dogs.Count + Customers.Count + Invoices.Count;
    }
}
