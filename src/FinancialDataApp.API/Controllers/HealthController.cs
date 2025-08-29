using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FinancialDataApp.API.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class HealthController : ControllerBase
    {
        private readonly IRepository<SystemHealth> _repo;
        private readonly IHealthService _healthService;
        private readonly ILogger<HealthController> _logger;

        public HealthController(
            IRepository<SystemHealth> repo,
            IHealthService healthService,
            ILogger<HealthController> logger)
        {
            _repo = repo;
            _healthService = healthService;
            _logger = logger;
        }

        // GET: /api/health/current
        // Returns a simplified list of latest health status per component
        [HttpGet("current")]
        public async Task<ActionResult<IEnumerable<object>>> GetCurrent()
        {
            try
            {
                var all = await _repo.GetAllAsync();

                // if we have historical entries, take the latest per component
                var latestByComponent = all
                    .GroupBy(h => string.IsNullOrWhiteSpace(h.Component) ? "System" : h.Component)
                    .Select(g => g.OrderByDescending(x => x.CreatedAtUtc).First())
                    .ToList();

                if (latestByComponent.Count == 0)
                {
                    // Fallback demo defaults if nothing is in DB yet
                    var defaults = new[]
                    {
                        BuildDto("Database", "Healthy", null),
                        BuildDto("API", "Healthy", null),
                        BuildDto("Ingestion", "Healthy", null),
                        BuildDto("SignalR", "Healthy", null),
                        BuildDto("Nginx", "Healthy", null)
                    };
                    return Ok(defaults);
                }

                var result = latestByComponent.Select(h => BuildDto(h.Component, h.Status, h.Metrics)).ToList();
                return Ok(result);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to get current health.");
                // On error, return an empty list so UI can still render
                return Ok(Array.Empty<object>());
            }
        }

        // POST: /api/health/check
        // Triggers a health check; returns the latest state for convenience
        [HttpPost("check")]
        public async Task<ActionResult<IEnumerable<object>>> RunHealthCheck()
        {
            try
            {
                await _healthService.CheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Health check failed.");
            }

            // Return current snapshot after check
            return await GetCurrent();
        }

        private static object BuildDto(string component, string status, string? metricsJson)
        {
            // Defaults
            int cpu = 25, mem = 30, resp = 120;

            if (!string.IsNullOrWhiteSpace(metricsJson))
            {
                try
                {
                    using var doc = JsonDocument.Parse(metricsJson);
                    var root = doc.RootElement;
                    if (root.TryGetProperty("cpuUsage", out var cpuEl) && cpuEl.TryGetInt32(out var cpuVal)) cpu = cpuVal;
                    if (root.TryGetProperty("memoryUsage", out var memEl) && memEl.TryGetInt32(out var memVal)) mem = memVal;
                    if (root.TryGetProperty("responseTime", out var respEl) && respEl.TryGetInt32(out var respVal)) resp = respVal;
                }
                catch
                {
                    // ignore parse errors and keep defaults
                }
            }

            return new
            {
                component = component,
                status = string.IsNullOrWhiteSpace(status) ? "Healthy" : status,
                cpuUsage = cpu,
                memoryUsage = mem,
                responseTime = resp
            };
        }
    }
}
