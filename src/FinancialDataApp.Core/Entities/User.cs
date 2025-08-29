using System;
using System.Collections.Generic;

namespace FinancialDataApp.Core.Entities
{
    public class User : BaseEntity
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public byte[] PasswordHash { get; set; } = Array.Empty<byte>();
        public byte[] PasswordSalt { get; set; } = Array.Empty<byte>();
        public List<string> Roles { get; set; } = new();
        public bool IsActive { get; set; } = true;
    }
}
