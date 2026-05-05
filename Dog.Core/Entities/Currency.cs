

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class Currency : AuditableBaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;   // USD, EUR, SAR …
        public string Symbol { get; set; } = string.Empty;  // $, €, ﷼ …
        //public bool IsActive { get; set; } = true;
    }
}