using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;

namespace FinancialDataApp.API.Controllers
{
    public class PaginatedResponse<T>
    {
        public IEnumerable<T> Items { get; set; } = Enumerable.Empty<T>();
        public int TotalCount { get; set; }
        public int PageNumber { get; set; }
        public int PageSize { get; set; }
        public int TotalPages { get; set; }
    }

    [ApiController]
    [Route("api/[controller]")]
    public class DataSourcesController : ControllerBase
    {
        private readonly IDataSourceService _service;
        private readonly ILogger<DataSourcesController> _logger;

        public DataSourcesController(IDataSourceService service, ILogger<DataSourcesController> logger)
        {
            _service = service;
            _logger = logger;
        }

        // GET: /api/datasources
        [HttpGet]
        public async Task<ActionResult<PaginatedResponse<object>>> GetAll(
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? searchTerm = null)
        {
            try
            {
                var all = await _service.GetAllAsync();

                // simple in-memory filtering/pagination (ok for demo)
                if (!string.IsNullOrWhiteSpace(searchTerm))
                {
                    all = all.Where(s => s.Name.Contains(searchTerm, StringComparison.OrdinalIgnoreCase));
                }

                var list = all.ToList();
                var total = list.Count;
                var items = list
                    .Skip((pageNumber - 1) * pageSize)
                    .Take(pageSize)
                    .Select(MapToDto)
                    .ToList();

                return Ok(new PaginatedResponse<object>
                {
                    Items = items,
                    TotalCount = total,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = (int)Math.Ceiling(total / (double)pageSize)
                });
            }
            catch (Exception ex)
            {
                // If DB isn't ready yet, return empty page so UI can still render
                _logger.LogError(ex, "Failed to load data sources. Returning empty list.");
                return Ok(new PaginatedResponse<object>
                {
                    Items = Array.Empty<object>(),
                    TotalCount = 0,
                    PageNumber = pageNumber,
                    PageSize = pageSize,
                    TotalPages = 0
                });
            }
        }

        // POST: /api/datasources/{id}/test
        [HttpPost("{id:guid}/test")]
        public ActionResult<object> TestConnection(Guid id)
        {
            // Stubbed response for now
            return Ok(new { success = true, message = "Test executed (stub)" });
        }

        // POST: /api/datasources/{id}/sync
        [HttpPost("{id:guid}/sync")]
        public IActionResult Sync(Guid id)
        {
            // Stubbed response for now
            return Ok(new { success = true });
        }

        private static object MapToDto(DataSource s)
        {
            // Map backend entity to a shape similar to frontend expectation
            // Frontend expects fields: id, name, type, connectionString, isActive, configuration, lastSync, createdAt, updatedAt
            // Our entity has: Id (Guid), Name, Type (enum), Url, ApiKey, PollIntervalSeconds, IsActive, Config (json)
            return new
            {
                id = s.Id,
                name = s.Name,
                // Map enum to a generic label the UI can display; UI handles toLowerCase for badge classes
                type = s.Type.ToString(),
                connectionString = s.Url,
                isActive = s.IsActive,
                configuration = string.IsNullOrWhiteSpace(s.Config) ? new { } : TryParseJson(s.Config),
                lastSync = (string?)null,
                createdAt = s.CreatedAtUtc.ToString("o"),
                updatedAt = s.UpdatedAtUtc.ToString("o")
            };
        }

        private static object TryParseJson(string json)
        {
            try
            {
                return System.Text.Json.JsonSerializer.Deserialize<object>(json) ?? new { };
            }
            catch
            {
                return new { };
            }
        }
    }
}
