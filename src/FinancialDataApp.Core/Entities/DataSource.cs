using System;

namespace FinancialDataApp.Core.Entities
{
    public enum DataSourceType
    {
        Rss,
        ExternalApi
    }

    public class DataSource : BaseEntity
    {
        public string Name { get; set; } = string.Empty;
        public DataSourceType Type { get; set; }
        public string Url { get; set; } = string.Empty;
        public string? ApiKey { get; set; }
        public int PollIntervalSeconds { get; set; }
        public bool IsActive { get; set; } = true;
        public string? Config { get; set; } // JSON config
    }
}
