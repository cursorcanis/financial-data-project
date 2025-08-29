using System;

namespace FinancialDataApp.Core.Entities
{
    public class SystemHealth : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public string Component { get; set; } = string.Empty;
        public string Status { get; set; } = string.Empty;
        public string? Message { get; set; }
        public string? Metrics { get; set; } // JSON metrics
    }
}
