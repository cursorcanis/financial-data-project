using System;

namespace FinancialDataApp.Core.Entities
{
    public class AuditLog : BaseEntity
    {
        public Guid UserId { get; set; }
        public string Action { get; set; } = string.Empty;
        public string Entity { get; set; } = string.Empty;
        public Guid EntityId { get; set; }
        public string? Changes { get; set; } // JSON diff
        public string Ip { get; set; } = string.Empty;
        public string UA { get; set; } = string.Empty;
    }
}
