using System;
using System.Net.Http;
using System.ServiceModel.Syndication;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Hosting;

namespace FinancialDataApp.Infrastructure.Ingestion
{
    public class DataIngestionHostedService : IHostedService, IDisposable
    {
        private readonly IServiceProvider _services;
        private readonly ILogger<DataIngestionHostedService> _logger;
        private readonly HttpClient _httpClient;
        private Timer? _timer;

        public DataIngestionHostedService(IServiceProvider services, ILogger<DataIngestionHostedService> logger)
        {
            _services = services;
            _logger = logger;
            _httpClient = new HttpClient();
        }

        public Task StartAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Data ingestion hosted service starting.");
            _timer = new Timer(async _ => await DoWork(), null, TimeSpan.Zero, TimeSpan.FromMinutes(1));
            return Task.CompletedTask;
        }

        private async Task DoWork()
        {
            try
            {
                using var scope = _services.CreateScope();
                var dataSourceService = scope.ServiceProvider.GetRequiredService<IDataSourceService>();
                var realTimeDataService = scope.ServiceProvider.GetRequiredService<IRealTimeDataService>();
                var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();

                var sources = await dataSourceService.GetAllAsync();
                foreach (var source in sources)
                {
                    if (!source.IsActive) continue;

                    if (source.Type == DataSourceType.Rss)
                    {
                        try
                        {
                            using var reader = XmlReader.Create(source.Url);
                            var feed = SyndicationFeed.Load(reader);
                            foreach (var item in feed.Items)
                            {
                                await realTimeDataService.StoreAsync(new RealTimeData
                                {
                                    DataSourceId = source.Id,
                                    Symbol = item.Title.Text,
                                    Value = 0, // RSS doesn’t provide numerical data directly
                                    TimestampUtc = DateTime.UtcNow,
                                    Payload = item.Summary?.Text
                                });
                            }
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error ingesting RSS feed.");
                        }
                    }
                    else if (source.Type == DataSourceType.ExternalApi)
                    {
                        try
                        {
                            var response = await _httpClient.GetStringAsync(source.Url);
                            await realTimeDataService.StoreAsync(new RealTimeData
                            {
                                DataSourceId = source.Id,
                                Symbol = source.Name,
                                Value = 0,
                                TimestampUtc = DateTime.UtcNow,
                                Payload = response
                            });
                        }
                        catch (Exception ex)
                        {
                            _logger.LogError(ex, "Error ingesting API data.");
                        }
                    }
                }

                await healthService.CheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Unexpected error in ingestion cycle.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("Data ingestion hosted service stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _httpClient.Dispose();
            _timer?.Dispose();
        }
    }
}
