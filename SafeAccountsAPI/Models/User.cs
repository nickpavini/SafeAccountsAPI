using System.Collections.Generic;
using Newtonsoft.Json;
using SafeAccountsAPI.Helpers;

namespace SafeAccountsAPI.Models
{
    public class User
    {
        public int ID { get; set; }
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string Email { get; set; }
        public byte[] Password { get; set; }
        public int NumAccs { get; set; }
        public string Role { get; set; }
        public bool EmailVerified { get; set; }
        public virtual List<Account> Accounts { get; set; }
        public virtual List<RefreshToken> RefreshTokens { get; set; }
        public virtual List<Folder> Folders { get; set; }

        public User() { } // blank constructor needed for db initializer

        // constructor to easily set from NewUser type
        public User(NewUser newUser)
        {
            First_Name = newUser.First_Name;
            Last_Name = newUser.Last_Name;
            Email = newUser.Email;
            Password = HelperMethods.ConcatenatedSaltAndSaltedHash(newUser.Password);
            NumAccs = 0;
            Role = UserRoles.User;
            EmailVerified = false;
        }
    }

    // this class exists so we can easily send the needed user data, but have more data server side
    public class ReturnableUser
    {
        public int ID { get; set; }
        public string First_Name { get; set; }
        public string Last_Name { get; set; }
        public string Email { get; set; }
        public int NumAccs { get; set; }
        public string Role { get; set; }

        public ReturnableUser() { }

        // constructor create a safe returnable user
        public ReturnableUser(User user)
        {
            ID = user.ID;
            First_Name = user.First_Name;
            Last_Name = user.Last_Name;
            Email = user.Email;
            NumAccs = user.NumAccs;
            Role = user.Role;
        }
    }

    // model for registering a new user
    public class NewUser
    {
        [JsonProperty]
        public string First_Name { get; set; }
        [JsonProperty]
        public string Last_Name { get; set; }
        [JsonProperty]
        public string Email { get; set; }
        [JsonProperty]
        public string Password { get; set; }
    }

    // static class for roles
    public static class UserRoles
    {
        public static string User = "user";
        public static string Admin = "admin";
    }
}
