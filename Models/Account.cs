using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace SafeAccountsAPI.Models
{
    public class Account
    {
        public int ID { get; set; }
        public string UserID { get; set; }
        public string Title { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
    }
}
