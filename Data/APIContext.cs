using Microsoft.EntityFrameworkCore;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Data
{
    // reference to the api database.. models refer to table entries
    public class APIContext : DbContext
    {
        private string _connectionString = "Server=DESKTOP-UB8RIFR\\SQLEXPRESS;Database=SafeAccountsAPI_Db;Trusted_Connection=True;MultipleActiveResultSets=true";

        public APIContext(DbContextOptions<APIContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseLazyLoadingProxies()
                .UseSqlServer(_connectionString);

        public DbSet<User> Users { get; set; } // whole table reference
        public DbSet<Account> Accounts { get; set; } // whole table reference
        public DbSet<RefreshToken> RefreshTokens { get; set; }
    }
}
