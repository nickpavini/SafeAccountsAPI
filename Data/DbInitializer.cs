using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using SafeAccountsAPI.Models;
using System.Threading.Tasks;
using System.Text;
using SafeAccountsAPI.Controllers;

namespace SafeAccountsAPI.Data
{
    public class DbInitializer
    {
        public static void Initialize(APIContext context)
        {
            context.Database.EnsureCreated();

            // Look for any students.
            if (!context.Users.Any())
            {
                // add base users if data base not populated
                var users = new User[]
                {
                    new User { First_Name="John", Last_Name="Doe", Email="john@doe.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User },
                    new User { First_Name="Edwin", Last_Name="May", Email="edwin@may.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User },
                    new User { First_Name="Lucy", Last_Name="Vale", Email="lucy@vale.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User },
                    new User { First_Name="Pam", Last_Name="Willis", Email="pam@willis.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User },
                    new User { First_Name="Game", Last_Name="Stonk", Email="game@stonk.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User }
                };

                foreach (User person in users) { context.Users.Add(person); } // add each user to the table
                context.SaveChanges(); // execute changes
            }

            if (!context.Accounts.Any())
            {
                // add 2 base accounts to each user for testing
                var accs = new Account[]
                {
                    new Account { UserID=1, Title="gmail", Login="johndoe", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.." },
                    new Account { UserID=1, Title="yahoo", Login="johndoe", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.." },
                    new Account { UserID=2, Title="paypal", Login="edwinmay", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.." },
                    new Account { UserID=2, Title="zoom", Login="edwinmay", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.." },
                    new Account { UserID=3, Title="chase", Login="lucyvale", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.."},
                    new Account { UserID=3, Title="netflix", Login="lucyvale", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.." },
                    new Account { UserID=4, Title="hulu", Login="pamwillis", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.." },
                    new Account { UserID=4, Title="amazon", Login="pamwillis", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.." },
                    new Account { UserID=5, Title="spotify", Login="gamestonk", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.." },
                    new Account { UserID=5, Title="bestbuy", Login="gamestonk", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.temp_password_key), Description="Add description here.."}
                };

                foreach (Account acc in accs) { context.Accounts.Add(acc); } // add each account to the table
                context.SaveChanges(); // execute changes
            }

            //This raw code isnt working for some reason with EnityFramework
            //context.Database.ExecuteSqlRaw(
            //        @"INSERT INTO TABLE Users (First_Name, Last_Name, Email, Password, NumAccs) Values (""Bob"", ""Jones"", ""Bob@Jones.com"", ""Useless"", 0)"
            //);
        }
    }
}
