using System;

namespace SafeAccountsAPI.Models
{
    public class User
    {
        public int ID { get; set; }
        public string User_Name { get; set; }
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string Email { get; set; }
        public string Password { get; set; }
        public int NumAccs { get; set; }
    }
}
