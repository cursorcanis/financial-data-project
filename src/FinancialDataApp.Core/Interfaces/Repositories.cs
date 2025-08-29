using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;

namespace FinancialDataApp.Core.Interfaces
{
    public interface IRepository<T> where T : BaseEntity
    {
        Task<T?> GetByIdAsync(Guid id);
        Task<IEnumerable<T>> GetAllAsync();
        Task AddAsync(T entity);
        void Update(T entity);
        void Remove(T entity);
    }

    public interface IUnitOfWork
    {
        Task<int> SaveChangesAsync();
    }
}
