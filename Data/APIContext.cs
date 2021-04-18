using Microsoft.EntityFrameworkCore;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Data
{
    // reference to the api database.. models refer to table entries
    public class APIContext : DbContext
    {
        public static readonly string _connectionString = "Server=safeaccounts.mysql.database.azure.com; Port=3306; Database=SafeAccountsAPI_Db; Uid=safeaccounts@safeaccounts; Pwd=Lqxx34pTqUpoIlRANDfC; SslMode=Preferred;";

        public APIContext(DbContextOptions<APIContext> options) : base(options)
        {
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
            => optionsBuilder
                .UseLazyLoadingProxies()
                .UseMySql(_connectionString, ServerVersion.AutoDetect(_connectionString));

        public DbSet<User> Users { get; set; } // whole table reference
        public DbSet<Account> Accounts { get; set; } // whole table reference
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Folder> Folders { get; set; }
    }
}
