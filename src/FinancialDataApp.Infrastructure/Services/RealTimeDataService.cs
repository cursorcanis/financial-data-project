using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;

namespace FinancialDataApp.Infrastructure.Services
{
    public class RealTimeDataService : IRealTimeDataService
    {
        private readonly IRepository<RealTimeData> _repository;
        private readonly IUnitOfWork _unitOfWork;

        public RealTimeDataService(IRepository<RealTimeData> repository, IUnitOfWork unitOfWork)
        {
            _repository = repository;
            _unitOfWork = unitOfWork;
        }

        public async Task StoreAsync(RealTimeData data)
        {
            await _repository.AddAsync(data);
            await _unitOfWork.SaveChangesAsync();
        }

        public async Task<IEnumerable<RealTimeData>> QueryAsync(string symbol)
        {
            var allData = await _repository.GetAllAsync();
            return allData.Where(d => d.Symbol == symbol).OrderByDescending(d => d.TimestampUtc);
        }
    }
}
