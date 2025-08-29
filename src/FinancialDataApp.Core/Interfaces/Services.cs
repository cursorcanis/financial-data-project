using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;

namespace FinancialDataApp.Core.Interfaces
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string username, string password);
        Task<User> RegisterAsync(User user, string password);
    }

    public interface IDataSourceService
    {
        Task<IEnumerable<DataSource>> GetAllAsync();
        Task<DataSource?> GetByIdAsync(Guid id);
    }

    public interface IRealTimeDataService
    {
        Task StoreAsync(RealTimeData data);
        Task<IEnumerable<RealTimeData>> QueryAsync(string symbol);
    }

    public interface IHealthService
    {
        Task<SystemHealth> CheckAsync();
    }

    public interface IAuditService
    {
        Task LogAsync(AuditLog entry);
    }

    public interface IDynamicFieldService
    {
        Task<IEnumerable<DynamicField>> GetFieldsAsync(Guid entityId);
    }

    public interface IExportService
    {
        Task<byte[]> ExportAsync<T>(IEnumerable<T> data);
    }

    public interface IIngestionRateTracker
    {
        void Track(string symbol);
        double GetRate(string symbol);
    }
}
