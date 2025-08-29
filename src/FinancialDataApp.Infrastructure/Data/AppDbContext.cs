using FinancialDataApp.Core.Entities;
using Microsoft.EntityFrameworkCore;

namespace FinancialDataApp.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<User> Users => Set<User>();
        public DbSet<DataSource> DataSources => Set<DataSource>();
        public DbSet<RealTimeData> RealTimeData => Set<RealTimeData>();
        public DbSet<SystemHealth> SystemHealth => Set<SystemHealth>();
        public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
        public DbSet<DynamicField> DynamicFields => Set<DynamicField>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // User unique indexes
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Email)
                .IsUnique();
            modelBuilder.Entity<User>()
                .HasIndex(u => u.Username)
                .IsUnique();

            // RealTimeData index on Symbol + Timestamp
            modelBuilder.Entity<RealTimeData>()
                .HasIndex(d => new { d.Symbol, d.TimestampUtc });

            // DataSource index on IsActive
            modelBuilder.Entity<DataSource>()
                .HasIndex(d => d.IsActive);
        }
    }
}
