using System;

namespace FinancialDataApp.Core.Entities
{
    public abstract class BaseEntity
    {
        public Guid Id { get; set; }
        public DateTime CreatedAtUtc { get; set; }
        public DateTime UpdatedAtUtc { get; set; }
        public string? Extra { get; set; } // JSON-serialized metadata
    }
}
