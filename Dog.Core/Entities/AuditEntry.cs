using Microsoft.EntityFrameworkCore.ChangeTracking;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace Dog.Core.Entities
{
    public class AuditEntry
    {
        [Key]
        [DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public long Id { get; set; }
        public string? EntityName { get; set; }
        public string? ActionType { get; set; }
        public string? Username { get; set; }
        public DateTime TimeStamp { get; set; }
        public string? EntityId { get; set; }
        public Dictionary<string, object>? Changes { get; set; }
        [NotMapped]
        public List<PropertyEntry>? TempProperties { get; set; }
    }

}