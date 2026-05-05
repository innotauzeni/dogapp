namespace Dog.Portal.Models
{
    public class DashboardViewModel
    {
        // KPI counts
        public int TotalDogs { get; set; }
        public int ActiveDogs { get; set; }
        public int TotalCustomers { get; set; }
        public int TotalLitters { get; set; }
        public int TotalMatingRecords { get; set; }
        public int TotalDeployments { get; set; }
        public int ActiveDeployments { get; set; }
        public int TotalBanks { get; set; }
        public int TotalBankAccounts { get; set; }

        // Recent items for activity feed
        public List<DashboardDogItem> RecentDogs { get; set; } = new();
        public List<DashboardDeploymentItem> RecentDeployments { get; set; } = new();
        public List<DashboardCustomerItem> RecentCustomers { get; set; } = new();

        // Breed distribution (top 5)
        public List<BreedCount> TopBreeds { get; set; } = new();

        // Gender split
        public int MaleDogs { get; set; }
        public int FemaleDogs { get; set; }

        // Error message if any API call failed
        public string? FetchError { get; set; }
    }

    public class DashboardDogItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public bool IsActive { get; set; }
        public DateTime DateOfBirth { get; set; }
    }

    public class DashboardDeploymentItem
    {
        public int Id { get; set; }
        public int DogId { get; set; }
        public string Location { get; set; } = string.Empty;
        public string CompanyDeployedTo { get; set; } = string.Empty;
        public string SecurityGuardAssigned { get; set; } = string.Empty;
        public DateTime StartDate { get; set; }
        public bool IsActive { get; set; }
    }

    public class DashboardCustomerItem
    {
        public int Id { get; set; }
        public string? FullName { get; set; }
        public string? Email { get; set; }
        public string? Cellphone { get; set; }
    }

    public class DashboardAnimalItem
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Breed { get; set; } = string.Empty;
        public string? Gender { get; set; }
        public bool IsActive { get; set; }
        public DateTime BirthDate { get; set; }
    }

    public class DashboardAnimalPage
    {
        public List<DashboardAnimalItem> Data { get; set; } = new();
        public int Total { get; set; }
    }

    public class BreedCount
    {
        public string Breed { get; set; } = string.Empty;
        public int Count { get; set; }
        public int Percentage { get; set; }
    }
}
