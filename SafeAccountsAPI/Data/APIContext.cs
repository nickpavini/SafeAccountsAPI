using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Data
{
    // reference to the api database.. models refer to table entries
    public class APIContext : DbContext
    {
        public IConfiguration Configuration { get; }

        public APIContext(DbContextOptions<APIContext> options, IConfiguration configuration) : base(options)
        {
            Configuration = configuration;
        }

        protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        {
            optionsBuilder
                .UseLazyLoadingProxies()
                .UseMySql(Configuration.GetValue<string>("ConnectionStrings:DefaultConnection"), ServerVersion.AutoDetect(Configuration.GetValue<string>("ConnectionStrings:DefaultConnection")));
        }

        public DbSet<User> Users { get; set; } // whole table reference
        public DbSet<Account> Accounts { get; set; } // whole table reference
        public DbSet<RefreshToken> RefreshTokens { get; set; }
        public DbSet<Folder> Folders { get; set; }
    }
}
