using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;
using FinancialDataApp.Infrastructure.Ingestion;
using FinancialDataApp.Infrastructure.Seeding;
using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace FinancialDataApp.API.Hosted
{
    /// <summary>
    /// Emits randomized demo data and SignalR updates when IngestionOptions.DemoMode == true.
    /// - Periodically generates RealTimeData for a set of symbols and stores them.
    /// - Sends IngestionUpdate and HealthUpdate messages over the DataHub.
    /// </summary>
    public class DemoSeederHostedService : IHostedService, IDisposable
    {
        private readonly ILogger<DemoSeederHostedService> _logger;
        private readonly IServiceProvider _services;
        private readonly IOptions<IngestionOptions> _options;
        private readonly IHubContext<DataHub> _dataHub;
        private Timer? _timer;
        private readonly Random _rng = new();

        // State for ingestion metrics
        private int _totalRecords = 0;

        // Remember last prices per symbol for random-walk generation
        private readonly Dictionary<string, decimal> _lastPrices = new(StringComparer.OrdinalIgnoreCase);

        public DemoSeederHostedService(
            ILogger<DemoSeederHostedService> logger,
            IServiceProvider services,
            IOptions<IngestionOptions> options,
            IHubContext<DataHub> dataHub)
        {
            _logger = logger;
            _services = services;
            _options = options;
            _dataHub = dataHub;
        }

        public async Task StartAsync(CancellationToken cancellationToken)
        {
            if (!_options.Value.DemoMode)
            {
                _logger.LogInformation("DemoSeederHostedService: DemoMode is disabled. Service will not start.");
                return;
            }

            _logger.LogInformation("DemoSeederHostedService: DemoMode enabled. Seeding initial data and starting generator.");

            // Ensure initial seed exists
            using (var scope = _services.CreateScope())
            {
                var seeder = scope.ServiceProvider.GetRequiredService<DemoDataSeeder>();
                await seeder.SeedDataSourcesAsync(cancellationToken);
                await seeder.SeedHistoricalAsync(cancellationToken);

                // Initialize last prices from recent historical if available
                var rtRepo = scope.ServiceProvider.GetRequiredService<IRepository<RealTimeData>>();
                var recent = (await rtRepo.GetAllAsync())
                    .GroupBy(r => r.Symbol)
                    .Select(g => g.OrderByDescending(x => x.TimestampUtc).First())
                    .ToList();

                foreach (var r in recent)
                {
                    if (!_lastPrices.ContainsKey(r.Symbol))
                        _lastPrices[r.Symbol] = r.Value;
                }
            }

            // Start periodic generation every 3 seconds
            _timer = new Timer(async _ => await Tick(), null, TimeSpan.Zero, TimeSpan.FromSeconds(3));
        }

        private async Task Tick()
        {
            try
            {
                using var scope = _services.CreateScope();
                var dataSourceService = scope.ServiceProvider.GetRequiredService<IDataSourceService>();
                var realTimeService = scope.ServiceProvider.GetRequiredService<IRealTimeDataService>();
                var healthService = scope.ServiceProvider.GetRequiredService<IHealthService>();

                // Get active sources
                var sources = (await dataSourceService.GetAllAsync()).Where(s => s.IsActive).ToList();
                if (sources.Count == 0) return;

                // Choose symbols to emit this tick
                var symbolMap = GetDefaultSymbolMap();
                var chosen = PickRandomSymbols(symbolMap, count: _rng.Next(3, 7));

                var now = DateTime.UtcNow;
                var emitted = 0;
                foreach (var (symbol, sourceName) in chosen)
                {
                    var source = sources.FirstOrDefault(s => s.Name.Equals(sourceName, StringComparison.OrdinalIgnoreCase))
                                 ?? sources[_rng.Next(sources.Count)];

                    var price = NextRandomWalk(symbol);
                    await realTimeService.StoreAsync(new RealTimeData
                    {
                        DataSourceId = source.Id,
                        Symbol = symbol,
                        Value = price,
                        TimestampUtc = now,
                        Payload = null
                    });
                    emitted++;
                }

                // Update counters
                _totalRecords += emitted;
                var successCount = (int)Math.Round(emitted * RandomBetween(0.90, 0.98));
                var errorCount = emitted - successCount;

                // Broadcast ingestion update over SignalR
                await _dataHub.Clients.All.SendAsync("IngestionUpdate", new
                {
                    timestamp = now.ToString("o"),
                    rate = emitted,
                    totalRecords = _totalRecords,
                    successCount,
                    errorCount
                });

                // Emit a health entry (and broadcast a synthetic health update)
                await healthService.CheckAsync();
                var healthUpdate = RandomHealthUpdate();
                await _dataHub.Clients.All.SendAsync("HealthUpdate", healthUpdate);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "DemoSeederHostedService tick failed.");
            }
        }

        public Task StopAsync(CancellationToken cancellationToken)
        {
            _logger.LogInformation("DemoSeederHostedService stopping.");
            _timer?.Change(Timeout.Infinite, 0);
            return Task.CompletedTask;
        }

        public void Dispose()
        {
            _timer?.Dispose();
        }

        private Dictionary<string, string[]> GetDefaultSymbolMap() => new()
        {
            { "Equities Feed", new[] { "AAPL", "MSFT", "AMZN" } },
            { "FX Rates", new[] { "EURUSD", "USDJPY" } },
            { "Crypto Prices", new[] { "BTCUSD", "ETHUSD" } },
            { "Commodities", new[] { "XAUUSD" } }
        };

        private IEnumerable<(string symbol, string sourceName)> PickRandomSymbols(Dictionary<string, string[]> map, int count)
        {
            var flat = map.SelectMany(kvp => kvp.Value.Select(sym => (sym, kvp.Key))).ToList();
            for (int i = 0; i < count; i++)
            {
                yield return flat[_rng.Next(flat.Count)];
            }
        }

        private decimal NextRandomWalk(string symbol)
        {
            if (!_lastPrices.TryGetValue(symbol, out var prev))
            {
                prev = symbol switch
                {
                    "AAPL" => 185m,
                    "MSFT" => 400m,
                    "AMZN" => 140m,
                    "EURUSD" => 1.10m,
                    "USDJPY" => 145m,
                    "BTCUSD" => 60000m,
                    "ETHUSD" => 3000m,
                    "XAUUSD" => 1900m,
                    _ => (decimal)(50 + _rng.NextDouble() * 500)
                };
            }

            var drift = (decimal)(_rng.NextDouble() * 0.02 - 0.01); // -1%..+1%
            var meanRev = (decimal)(_rng.NextDouble() * 0.002 - 0.001);
            var next = prev * (1 + drift + meanRev);
            if (next <= 0) next = prev;
            _lastPrices[symbol] = Math.Round(next, 4);
            return _lastPrices[symbol];
        }

        private object RandomHealthUpdate()
        {
            var components = new[] { "Database", "API", "Ingestion", "SignalR", "Nginx" };
            var component = components[_rng.Next(components.Length)];
            var statusRoll = _rng.NextDouble();
            var status = statusRoll switch
            {
                < 0.8 => "Healthy",
                < 0.95 => "Degraded",
                _ => "Unhealthy"
            };

            return new
            {
                component,
                status,
                metrics = new
                {
                    cpuUsage = (int)_rng.Next(10, 85),
                    memoryUsage = (int)_rng.Next(15, 80),
                    responseTime = _rng.Next(20, 400)
                }
            };
        }

        private double RandomBetween(double min, double max) => min + _rng.NextDouble() * (max - min);
    }
}
