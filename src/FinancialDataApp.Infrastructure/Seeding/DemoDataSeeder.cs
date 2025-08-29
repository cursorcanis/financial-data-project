using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;

namespace FinancialDataApp.Infrastructure.Seeding
{
    /// <summary>
    /// Seeds demo DataSources and optional historical RealTimeData for demo mode.
    /// Uses repositories directly to avoid needing service interfaces with add methods.
    /// </summary>
    public class DemoDataSeeder
    {
        private readonly IRepository<DataSource> _dataSourceRepo;
        private readonly IRepository<RealTimeData> _realTimeRepo;
        private readonly IUnitOfWork _uow;
        private readonly Random _rng = new Random();

        public DemoDataSeeder(
            IRepository<DataSource> dataSourceRepo,
            IRepository<RealTimeData> realTimeRepo,
            IUnitOfWork uow)
        {
            _dataSourceRepo = dataSourceRepo;
            _realTimeRepo = realTimeRepo;
            _uow = uow;
        }

        /// <summary>
        /// Seed a default set of demo data sources if none exist.
        /// </summary>
        public async Task SeedDataSourcesAsync(CancellationToken ct = default)
        {
            var existing = await _dataSourceRepo.GetAllAsync();
            if (existing.Any())
            {
                return;
            }

            var sources = new List<DataSource>
            {
                new DataSource
                {
                    Id = Guid.NewGuid(),
                    Name = "Tech News",
                    Type = DataSourceType.Rss,
                    Url = "https://example.com/rss/tech",
                    PollIntervalSeconds = 60,
                    IsActive = true,
                    Config = "{}"
                },
                new DataSource
                {
                    Id = Guid.NewGuid(),
                    Name = "Market Headlines",
                    Type = DataSourceType.Rss,
                    Url = "https://example.com/rss/markets",
                    PollIntervalSeconds = 90,
                    IsActive = true,
                    Config = "{}"
                },
                new DataSource
                {
                    Id = Guid.NewGuid(),
                    Name = "Equities Feed",
                    Type = DataSourceType.ExternalApi,
                    Url = "https://api.example.com/equities",
                    PollIntervalSeconds = 15,
                    IsActive = true,
                    Config = "{}"
                },
                new DataSource
                {
                    Id = Guid.NewGuid(),
                    Name = "FX Rates",
                    Type = DataSourceType.ExternalApi,
                    Url = "https://api.example.com/fx",
                    PollIntervalSeconds = 20,
                    IsActive = true,
                    Config = "{}"
                },
                new DataSource
                {
                    Id = Guid.NewGuid(),
                    Name = "Crypto Prices",
                    Type = DataSourceType.ExternalApi,
                    Url = "https://api.example.com/crypto",
                    PollIntervalSeconds = 10,
                    IsActive = true,
                    Config = "{}"
                },
                new DataSource
                {
                    Id = Guid.NewGuid(),
                    Name = "Commodities",
                    Type = DataSourceType.ExternalApi,
                    Url = "https://api.example.com/commodities",
                    PollIntervalSeconds = 30,
                    IsActive = true,
                    Config = "{}"
                }
            };

            foreach (var s in sources)
            {
                await _dataSourceRepo.AddAsync(s);
            }
            await _uow.SaveChangesAsync();
        }

        /// <summary>
        /// Seed a small amount of historical price data for select symbols to make charts useful.
        /// </summary>
        public async Task SeedHistoricalAsync(CancellationToken ct = default)
        {
            // If there is already some data, skip to avoid duplication
            var existing = await _realTimeRepo.GetAllAsync();
            if (existing.Any())
            {
                return;
            }

            var allSources = (await _dataSourceRepo.GetAllAsync()).ToList();
            if (!allSources.Any()) return;

            // Map sources to a few representative symbols
            var symbolMap = new Dictionary<string, string[]>
            {
                { "Equities Feed", new[] { "AAPL", "MSFT", "AMZN" } },
                { "FX Rates", new[] { "EURUSD", "USDJPY" } },
                { "Crypto Prices", new[] { "BTCUSD", "ETHUSD" } },
                { "Commodities", new[] { "XAUUSD" } }
            };

            // Starting prices
            var starts = new Dictionary<string, decimal>
            {
                { "AAPL", 185.0m }, { "MSFT", 400.0m }, { "AMZN", 140.0m },
                { "EURUSD", 1.10m }, { "USDJPY", 145.0m },
                { "BTCUSD", 60000m }, { "ETHUSD", 3000m },
                { "XAUUSD", 1900m }
            };

            var end = DateTime.UtcNow;
            var start = end.AddHours(-2); // last 2 hours
            var step = TimeSpan.FromMinutes(1);

            foreach (var kvp in symbolMap)
            {
                var src = allSources.FirstOrDefault(s => s.Name.Equals(kvp.Key, StringComparison.OrdinalIgnoreCase));
                if (src == null) continue;

                foreach (var symbol in kvp.Value)
                {
                    var price = starts.TryGetValue(symbol, out var sp) ? sp : RandomRange(50m, 500m);
                    for (var t = start; t <= end; t = t.Add(step))
                    {
                        // random walk
                        price = NextRandomWalk(price);
                        var data = new RealTimeData
                        {
                            Id = Guid.NewGuid(),
                            DataSourceId = src.Id,
                            Symbol = symbol,
                            Value = Math.Round(price, 4),
                            TimestampUtc = t,
                            Payload = null
                        };
                        await _realTimeRepo.AddAsync(data);
                    }
                }
            }

            await _uow.SaveChangesAsync();
        }

        private decimal NextRandomWalk(decimal prev)
        {
            // +/- up to 1% move per step with a slight mean reversion
            var drift = (decimal)(_rng.NextDouble() * 0.02 - 0.01); // -1% .. +1%
            var meanReversion = (decimal)(_rng.NextDouble() * 0.002 - 0.001); // small pull
            var next = prev * (1 + drift + meanReversion);
            if (next <= 0) next = prev; // guard
            return next;
        }

        private decimal RandomRange(decimal min, decimal max)
        {
            return min + (decimal)_rng.NextDouble() * (max - min);
        }
    }
}
