using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SafeAccountsAPI.Models
{
    public class Account
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public string Title { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public User User { get; set; }
    }

    public class ReturnableAccount
    {
        public string Title { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }

        public ReturnableAccount(Account acc)
        {
            Title = acc.Title;
            Login = acc.Login;
            Password = acc.Password;
            Description = acc.Description;
        }
    }
}
