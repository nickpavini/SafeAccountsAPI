using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SafeAccountsAPI.Models
{
    public class Account
    {
        public int id { get; set; }
        public string userId { get; set; }
        public string title { get; set; }
        public string login { get; set; }
        public string password { get; set; }
        public string description { get; set; }
    }
}
