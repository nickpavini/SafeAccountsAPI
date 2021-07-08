using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Data
{
    public static class DbInitializer
    {
        public static void Initialize(APIContext context, IConfiguration config)
        {
            context.Database.EnsureCreated();

            // Look for any students.
            if (!context.Users.Any())
            {
                // set key and IV
                string[] keyAndIV = new string[] { config.GetValue<string>("UserEncryptionKey"), config.GetValue<string>("UserEncryptionIV") };

                // add base users if data base not populated
                User[] users = new User[]
                {
                    new User {
                        First_Name=HelperMethods.EncryptStringToBytes_Aes("John", keyAndIV),
                        Last_Name=HelperMethods.EncryptStringToBytes_Aes("Doe", keyAndIV),
                        Email=HelperMethods.EncryptStringToBytes_Aes("john@doe.com", keyAndIV),
                        Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"),
                        Role=HelperMethods.EncryptStringToBytes_Aes(UserRoles.User, keyAndIV),
                        EmailVerified=true
                    },
                    new User {
                        First_Name=HelperMethods.EncryptStringToBytes_Aes("Edwin", keyAndIV),
                        Last_Name=HelperMethods.EncryptStringToBytes_Aes("May", keyAndIV),
                        Email=HelperMethods.EncryptStringToBytes_Aes("edwin@may.com", keyAndIV),
                        Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"),
                        Role=HelperMethods.EncryptStringToBytes_Aes(UserRoles.User, keyAndIV),
                        EmailVerified=true
                    },
                    new User {
                        First_Name=HelperMethods.EncryptStringToBytes_Aes("Lucy", keyAndIV),
                        Last_Name=HelperMethods.EncryptStringToBytes_Aes("Vale", keyAndIV),
                        Email=HelperMethods.EncryptStringToBytes_Aes("lucy@vale.com", keyAndIV),
                        Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"),
                        Role=HelperMethods.EncryptStringToBytes_Aes(UserRoles.User, keyAndIV),
                        EmailVerified=true
                    },
                    new User {
                        First_Name=HelperMethods.EncryptStringToBytes_Aes("Pam", keyAndIV),
                        Last_Name=HelperMethods.EncryptStringToBytes_Aes("Willis", keyAndIV),
                        Email=HelperMethods.EncryptStringToBytes_Aes("pam@willis.com", keyAndIV),
                        Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"),
                        Role=HelperMethods.EncryptStringToBytes_Aes(UserRoles.User, keyAndIV),
                        EmailVerified=true
                    },
                    new User {
                        First_Name=HelperMethods.EncryptStringToBytes_Aes("Game", keyAndIV),
                        Last_Name=HelperMethods.EncryptStringToBytes_Aes("Stonk", keyAndIV),
                        Email=HelperMethods.EncryptStringToBytes_Aes("game@stonk.com", keyAndIV),
                        Password=HelperMethods.ConcatenatedSaltAndSaltedHash("useless"),
                        Role=HelperMethods.EncryptStringToBytes_Aes(UserRoles.User, keyAndIV),
                        EmailVerified=true
                    }
                };

                foreach (User person in users)
                    context.Users.Add(person); // add each user to the table

                context.SaveChanges(); // execute changes
            }

            /*
             * For now these accounts will be unencrypted just to ensure db connection..
             * For the testers, it is just the byte arrays
             *
             * Our client side application will be doing the encryption and the api will only ever send and recieve encrypted hex strings
             */
            if (!context.Accounts.Any())
            {
                // add 2 base accounts to each user for testing
                Account[] accs = new Account[]
                {
                    new Account {
                        UserID=1,
                        Title=Encoding.UTF8.GetBytes("gmail"),
                        Login=Encoding.UTF8.GetBytes("johndoe"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=1,
                        Title=Encoding.UTF8.GetBytes("yahoo"),
                        Login=Encoding.UTF8.GetBytes("johndoe"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=2,
                        Title=Encoding.UTF8.GetBytes("paypal"),
                        Login=Encoding.UTF8.GetBytes("edwinmay"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=2,
                        Title=Encoding.UTF8.GetBytes("zoom"),
                        Login=Encoding.UTF8.GetBytes("edwinmay"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=3,
                        Title=Encoding.UTF8.GetBytes("chase"),
                        Login=Encoding.UTF8.GetBytes("lucyvale"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=3,
                        Title=Encoding.UTF8.GetBytes("netflix"),
                        Login=Encoding.UTF8.GetBytes("lucyvale"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=4,
                        Title=Encoding.UTF8.GetBytes("hulu"),
                        Login=Encoding.UTF8.GetBytes("pamwillis"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=4,
                        Title=Encoding.UTF8.GetBytes("amazon"),
                        Login=Encoding.UTF8.GetBytes("pamwillis"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=5,
                        Title=Encoding.UTF8.GetBytes("spotify"),
                        Login=Encoding.UTF8.GetBytes("gamestonk"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=5,
                        Title=Encoding.UTF8.GetBytes("bestbuy"),
                        Login=Encoding.UTF8.GetBytes("gamestonk"),
                        Password=Encoding.UTF8.GetBytes("useless"),
                        Url=Encoding.UTF8.GetBytes("testurl.com"),
                        Description=Encoding.UTF8.GetBytes("description..."),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                };

                foreach (Account acc in accs) { context.Accounts.Add(acc); } // add each account to the table
                context.SaveChanges(); // execute changes
            }


            /*
             * For now these folders will be unencrypted just to ensure db connection..
             * For the testers, it is just the byte arrays
             *
             * Our client side application will be doing the encryption and the api will only ever send and recieve encrypted data
             */
            if (!context.Folders.Any())
            {
                // add base folders
                Folder[] base_folds = new Folder[]
                {
                    new Folder { UserID=1, FolderName=Encoding.UTF8.GetBytes("Folder"), HasChild =true },
                    new Folder { UserID=2, FolderName=Encoding.UTF8.GetBytes("Folder"), HasChild=true },
                    new Folder { UserID=3, FolderName=Encoding.UTF8.GetBytes("Folder"), HasChild=true },
                    new Folder { UserID=4, FolderName=Encoding.UTF8.GetBytes("Folder"), HasChild=true },
                    new Folder { UserID=5, FolderName=Encoding.UTF8.GetBytes("Folder"), HasChild=true }
                };

                Folder[] sub_folds = new Folder[]
                {
                    new Folder { UserID=1, FolderName=Encoding.UTF8.GetBytes("Sub-Folder"), HasChild=false, ParentID=5 },
                    new Folder { UserID=2, FolderName=Encoding.UTF8.GetBytes("Sub-Folder"), HasChild=false, ParentID=4 },
                    new Folder { UserID=3, FolderName=Encoding.UTF8.GetBytes("Sub-Folder"), HasChild=false, ParentID=3 },
                    new Folder { UserID=4, FolderName=Encoding.UTF8.GetBytes("Sub-Folder"), HasChild=false, ParentID=2 },
                    new Folder { UserID=5, FolderName=Encoding.UTF8.GetBytes("Sub-Folder"), HasChild=false, ParentID=1 }
                };

                foreach (Folder fold in base_folds) { context.Folders.Add(fold); } // add each account to the table
                context.SaveChanges();
                foreach (Folder fold in sub_folds) { context.Folders.Add(fold); } // add each account to the table
                context.SaveChanges(); // execute changes
            }
        }
    }
}
