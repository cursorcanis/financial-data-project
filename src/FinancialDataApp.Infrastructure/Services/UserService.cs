using System.Linq;
using System.Threading.Tasks;
using FinancialDataApp.Core.Entities;
using FinancialDataApp.Core.Interfaces;
using FinancialDataApp.Infrastructure.Security;

namespace FinancialDataApp.Infrastructure.Services
{
    public class UserService : IUserService
    {
        private readonly IRepository<User> _userRepository;
        private readonly IUnitOfWork _unitOfWork;

        public UserService(IRepository<User> userRepository, IUnitOfWork unitOfWork)
        {
            _userRepository = userRepository;
            _unitOfWork = unitOfWork;
        }

        public async Task<User?> AuthenticateAsync(string username, string password)
        {
            var users = await _userRepository.GetAllAsync();
            var user = users.FirstOrDefault(u => u.Username == username);
            if (user == null) return null;

            if (PasswordHasher.VerifyPassword(password, user.PasswordHash, user.PasswordSalt))
                return user;

            return null;
        }

        public async Task<User> RegisterAsync(User user, string password)
        {
            var (hash, salt) = PasswordHasher.HashPassword(password);
            user.PasswordHash = hash;
            user.PasswordSalt = salt;
            await _userRepository.AddAsync(user);
            await _unitOfWork.SaveChangesAsync();
            return user;
        }
    }
}
