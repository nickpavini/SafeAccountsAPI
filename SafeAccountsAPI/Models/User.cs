using System.Collections.Generic;
using Newtonsoft.Json;
using SafeAccountsAPI.Helpers;

namespace SafeAccountsAPI.Models
{
    public class User
    {
        public int ID { get; set; }
        public byte[] First_Name { get; set; }
        public byte[] Last_Name { get; set; }
        public byte[] Email { get; set; }
        public byte[] Password { get; set; }
        public byte[] Role { get; set; }
        public bool EmailVerified { get; set; }
        public virtual List<Account> Accounts { get; set; }
        public virtual List<RefreshToken> RefreshTokens { get; set; }
        public virtual List<Folder> Folders { get; set; }

        public User() { } // blank constructor needed for db initializer

        // constructor to easily set from NewUser type
        public User(NewUser newUser, string[] keyAndIV)
        {
            First_Name = HelperMethods.EncryptStringToBytes_Aes(newUser.First_Name, keyAndIV);
            Last_Name = HelperMethods.EncryptStringToBytes_Aes(newUser.Last_Name, keyAndIV);
            Email = HelperMethods.EncryptStringToBytes_Aes(newUser.Email, keyAndIV);
            Password = HelperMethods.ConcatenatedSaltAndSaltedHash(newUser.Password);
            Role = HelperMethods.EncryptStringToBytes_Aes(UserRoles.User, keyAndIV);
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
        public string Role { get; set; }

        public ReturnableUser() { }

        // constructor create a safe returnable user
        public ReturnableUser(User user, string[] keyAndIV)
        {
            ID = user.ID;
            First_Name = HelperMethods.DecryptStringFromBytes_Aes(user.First_Name, keyAndIV);
            Last_Name = HelperMethods.DecryptStringFromBytes_Aes(user.Last_Name, keyAndIV);
            Email = HelperMethods.DecryptStringFromBytes_Aes(user.Email, keyAndIV);
            Role = HelperMethods.DecryptStringFromBytes_Aes(user.Role, keyAndIV);
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
