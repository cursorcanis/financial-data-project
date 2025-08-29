using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using FinancialDataApp.Core.Interfaces;

namespace FinancialDataApp.Infrastructure.Services
{
    public class ExportService : IExportService
    {
        public async Task<byte[]> ExportAsync<T>(IEnumerable<T> data)
        {
            var sb = new StringBuilder();
            var props = typeof(T).GetProperties();

            // Header row
            foreach (var prop in props)
            {
                sb.Append(prop.Name).Append(",");
            }
            sb.Length--; // remove last comma
            sb.AppendLine();

            // Data rows
            foreach (var item in data)
            {
                foreach (var prop in props)
                {
                    var value = prop.GetValue(item)?.ToString() ?? string.Empty;
                    sb.Append(value.Replace(",", ";")).Append(",");
                }
                sb.Length--; // remove last comma
                sb.AppendLine();
            }

            return await Task.FromResult(Encoding.UTF8.GetBytes(sb.ToString()));
        }
    }
}
