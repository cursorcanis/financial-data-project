using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;

namespace FinancialDataApp.Infrastructure.Services
{
    public class AuditService : IAuditService
    {
        private readonly IRepository<AuditLog> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public AuditService(IRepository<AuditLog> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task LogAsync(AuditLog entry)
        {
            await _repository.AddAsync(entry);
            await _unitOfWork.SaveChangesAsync();
        }
    }
}
