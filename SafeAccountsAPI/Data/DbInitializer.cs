using System;
using System.Linq;
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Data
{
    public static class DbInitializer
    {
        public static void Initialize(APIContext context)
        {
            context.Database.EnsureCreated();

            // Look for any students.
            if (!context.Users.Any())
            {
                // add base users if data base not populated
                User[] users = new User[]
                {
                    new User { First_Name="John", Last_Name="Doe", Email="john@doe.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User, EmailVerified=true },
                    new User { First_Name="Edwin", Last_Name="May", Email="edwin@may.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User, EmailVerified=true },
                    new User { First_Name="Lucy", Last_Name="Vale", Email="lucy@vale.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User, EmailVerified=true },
                    new User { First_Name="Pam", Last_Name="Willis", Email="pam@willis.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User, EmailVerified=true },
                    new User { First_Name="Game", Last_Name="Stonk", Email="game@stonk.com", Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"), NumAccs=2, Role=UserRoles.User, EmailVerified=true }
                };

                foreach (User person in users)
                    context.Users.Add(person); // add each user to the table

                context.SaveChanges(); // execute changes

                // create a key and iv for these base users
                foreach (User person in context.Users)
                    HelperMethods.CreateUserKeyandIV(person.ID);
            }

            if (!context.Accounts.Any())
            {
                // add 2 base accounts to each user for testing
                Account[] accs = new Account[]
                {
                    new Account { UserID=1, Title="gmail", Login="johndoe", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(1)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false },
                    new Account { UserID=1, Title="yahoo", Login="johndoe", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(1)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false },
                    new Account { UserID=2, Title="paypal", Login="edwinmay", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(2)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false },
                    new Account { UserID=2, Title="zoom", Login="edwinmay", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(2)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false },
                    new Account { UserID=3, Title="chase", Login="lucyvale", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(3)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false },
                    new Account { UserID=3, Title="netflix", Login="lucyvale", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(3)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false },
                    new Account { UserID=4, Title="hulu", Login="pamwillis", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(4)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false },
                    new Account { UserID=4, Title="amazon", Login="pamwillis", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(4)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false },
                    new Account { UserID=5, Title="spotify", Login="gamestonk", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(5)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false },
                    new Account { UserID=5, Title="bestbuy", Login="gamestonk", Password=HelperMethods.EncryptStringToBytes_Aes("useless", HelperMethods.GetUserKeyAndIV(5)), Url="testurl.com", Description="Add description here..", LastModified=DateTime.Now.ToString(), IsFavorite=false }
                };

                foreach (Account acc in accs) { context.Accounts.Add(acc); } // add each account to the table
                context.SaveChanges(); // execute changes
            }

            if (!context.Folders.Any())
            {
                // add base folders
                Folder[] base_folds = new Folder[]
                {
                    new Folder { UserID=1, FolderName="Folder", HasChild=true },
                    new Folder { UserID=2, FolderName="Folder", HasChild=true },
                    new Folder { UserID=3, FolderName="Folder", HasChild=true },
                    new Folder { UserID=4, FolderName="Folder", HasChild=true },
                    new Folder { UserID=5, FolderName="Folder", HasChild=true }
                };

                Folder[] sub_folds = new Folder[]
                {
                    new Folder { UserID=1, FolderName="Sub-Folder", HasChild=false, ParentID=5 },
                    new Folder { UserID=2, FolderName="Sub-Folder", HasChild=false, ParentID=4 },
                    new Folder { UserID=3, FolderName="Sub-Folder", HasChild=false, ParentID=3 },
                    new Folder { UserID=4, FolderName="Sub-Folder", HasChild=false, ParentID=2 },
                    new Folder { UserID=5, FolderName="Sub-Folder", HasChild=false, ParentID=1 }
                };

                foreach (Folder fold in base_folds) { context.Folders.Add(fold); } // add each account to the table
                context.SaveChanges();
                foreach (Folder fold in sub_folds) { context.Folders.Add(fold); } // add each account to the table
                context.SaveChanges(); // execute changes
            }
        }
    }
}
