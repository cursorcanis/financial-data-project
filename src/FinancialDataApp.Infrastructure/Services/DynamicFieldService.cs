using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;

namespace FinancialDataApp.Infrastructure.Services
{
    public class DynamicFieldService : IDynamicFieldService
    {
        private readonly IRepository<DynamicField> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public DynamicFieldService(IRepository<DynamicField> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<IEnumerable<DynamicField>> GetFieldsAsync(Guid entityId)
        {
            var fields = await _repository.GetAllAsync();
            return fields.Where(f => f.Extra == entityId.ToString());
        }

        public async Task AddFieldAsync(DynamicField field, Guid entityId)
        {
            field.Extra = entityId.ToString();
            await _repository.AddAsync(field);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task UpdateFieldAsync(DynamicField field)
        {
            _repository.Update(field);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task DeleteFieldAsync(DynamicField field)
        {
            _repository.Remove(field);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
