using System;
using System.Security.Cryptography;
using System.Text;

namespace FinancialDataApp.Infrastructure.Security
{
    public static class PasswordHasher
    {
        private const int SaltSize = 16; // 128 bit
        private const int KeySize = 32;  // 256 bit
        private const int Iterations = 10000;

        public static (byte[] hash, byte[] salt) HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(SaltSize);
            using var rfc2898 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] hash = rfc2898.GetBytes(KeySize);
            return (hash, salt);
        }

        public static bool VerifyPassword(string password, byte[] hash, byte[] salt)
        {
            using var rfc2898 = new Rfc2898DeriveBytes(password, salt, Iterations, HashAlgorithmName.SHA256);
            byte[] attemptedHash = rfc2898.GetBytes(KeySize);
            return CryptographicOperations.FixedTimeEquals(hash, attemptedHash);
        }
    }
}
