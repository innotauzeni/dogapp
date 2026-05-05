

using Dog.Core.Common;

namespace Dog.Core.Entities
{
    public class AlertSetting : AuditableEntity, IAuditable
    {
        public string? Event { get; set; }  //Subscription
        public string? Subject { get; set; } //Subscription Information
        public string? Template { get; set; } //Dear {{first_name}}, you have {{due_amount}} Subscription. please deposit on time. For more info contact the Account Department.
        public int Email { get; set; } = 1;   //1
        public int SMS { get; set; } = 0;    // 1
        public int Status { get; set; } = 1; //1
    }
}
