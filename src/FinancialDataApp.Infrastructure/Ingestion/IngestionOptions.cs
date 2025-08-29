namespace FinancialDataApp.Infrastructure.Ingestion
{
    public class IngestionOptions
    {
        public int DefaultPollIntervalSeconds { get; set; } = 60;
        public int RetryCount { get; set; } = 3;
        public int RetryDelaySeconds { get; set; } = 5;

        // Demo mode flag to enable randomized test data generation
        public bool DemoMode { get; set; } = false;
    }
}
