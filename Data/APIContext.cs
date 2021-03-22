using Microsoft.EntityFrameworkCore;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Data
{
    // reference to the api database.. models refer to table entries
    public class APIContext : DbContext
    {
        public APIContext(DbContextOptions<APIContext> options) : base(options)
        {
        }

        public DbSet<User> Users { get; set; } // whole table reference
        public DbSet<Account> Accounts { get; set; } // whole table reference
    }
}
