using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;

namespace FinancialDataApp.Infrastructure.Services
{
    public class DataSourceService : IDataSourceService
    {
        private readonly IRepository<DataSource> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DataSourceService(IRepository<DataSource> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<DataSource>> GetAllAsync()
        {
            return await _repository.GetAllAsync();
        }

        public async Task<DataSource?> GetByIdAsync(Guid id)
        {
            return await _repository.GetByIdAsync(id);
        }

        public async Task<DataSource> AddAsync(DataSource source)
        {
            await _repository.AddAsync(source);
            await _unitOfWork.SaveChangesAsync();
            return source;
        }

        public async Task UpdateAsync(DataSource source)
        {
            _repository.Update(source);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteAsync(DataSource source)
        {
            _repository.Remove(source);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
