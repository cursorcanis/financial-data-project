using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;

namespace FinancialDataApp.Infrastructure.Services
{
    public class HealthService : IHealthService
    {
        private readonly IRepository<SystemHealth> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public HealthService(IRepository<SystemHealth> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task<SystemHealth> CheckAsync()
        {
            // For now, just return a generic health check entity.
            var health = new SystemHealth
            {
                Name = "System",
                Component = "Infrastructure",
                Status = "Healthy",
                Message = "All systems nominal"
            };

            await _repository.AddAsync(health);
            await _unitOfWork.SaveChangesAsync();
            return health;
        }
    }
}
