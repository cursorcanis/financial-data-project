using System;

namespace FinancialDataApp.Core.Entities
{
    public class RealTimeData : BaseEntity
    {
        public Guid DataSourceId { get; set; }
        public string Symbol { get; set; } = string.Empty;
        public decimal Value { get; set; }
        public DateTime TimestampUtc { get; set; }
        public string? Payload { get; set; } // JSON payload
    }
}
