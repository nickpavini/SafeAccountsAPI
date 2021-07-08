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
                        Title=HelperMethods.HexStringToByteArray("9625B5E80B10566585FF0DA0B856AD68"),
                        Login=HelperMethods.HexStringToByteArray("C5394419056C9081A2FAF6CE6AF8ACB0"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=1,
                        Title=HelperMethods.HexStringToByteArray("91CB16829F918EA24FA9857664E25A27"),
                        Login=HelperMethods.HexStringToByteArray("C5394419056C9081A2FAF6CE6AF8ACB0"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=2,
                        Title=HelperMethods.HexStringToByteArray("F67714B3ACC7A6FD485B55FC65C75F66"),
                        Login=HelperMethods.HexStringToByteArray("70F663B0D722D99DF67B0A9BD654FC93"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=2,
                        Title=HelperMethods.HexStringToByteArray("E8A18F66E16ABA40F2AA92B1227D1BF5"),
                        Login=HelperMethods.HexStringToByteArray("70F663B0D722D99DF67B0A9BD654FC93"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=3,
                        Title=HelperMethods.HexStringToByteArray("27041EC622E17377F7E5B822D2940611"),
                        Login=HelperMethods.HexStringToByteArray("C89B1CFEA640A606A9DAB0FAD7D47703"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=3,
                        Title=HelperMethods.HexStringToByteArray("DA5932D26A6AF3072DDBAC82E13F6AD0"),
                        Login=HelperMethods.HexStringToByteArray("C89B1CFEA640A606A9DAB0FAD7D47703"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=4,
                        Title=HelperMethods.HexStringToByteArray("9FC71B6C5345C1C5A9DDBDAC3A7FECB5"),
                        Login=HelperMethods.HexStringToByteArray("9B09A134F0A80D163FE87DB4DB9F4430"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=4,
                        Title=HelperMethods.HexStringToByteArray("9CFF0E4F9407B0EB81366AB06B687AF0"),
                        Login=HelperMethods.HexStringToByteArray("9B09A134F0A80D163FE87DB4DB9F4430"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=5,
                        Title=HelperMethods.HexStringToByteArray("301712F5E57EFD4AC79F62B26F79A654"),
                        Login=HelperMethods.HexStringToByteArray("8CCFED303762BEB13C916CDD40354624"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
                        LastModified=DateTime.Now.ToString(),
                        IsFavorite=false
                    },
                    new Account {
                        UserID=5,
                        Title=HelperMethods.HexStringToByteArray("06AEBF7F5BDE9A3BF468D91BE81C032D"),
                        Login=HelperMethods.HexStringToByteArray("8CCFED303762BEB13C916CDD40354624"),
                        Password=HelperMethods.HexStringToByteArray("231D60897ACF5DF1DA18F9D9092B50F1"),
                        Url=HelperMethods.HexStringToByteArray("0088D5E4F29C898BEEA3535D770B4AFA"),
                        Description=HelperMethods.HexStringToByteArray("38579F1B9D124E83694AB75716E97E1C"),
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
                    new Folder { UserID=1, FolderName=HelperMethods.HexStringToByteArray("F6E3C1C45DDCC44CB50355BC6EE1FD08"), HasChild =true },
                    new Folder { UserID=2, FolderName=HelperMethods.HexStringToByteArray("F6E3C1C45DDCC44CB50355BC6EE1FD08"), HasChild=true },
                    new Folder { UserID=3, FolderName=HelperMethods.HexStringToByteArray("F6E3C1C45DDCC44CB50355BC6EE1FD08"), HasChild=true },
                    new Folder { UserID=4, FolderName=HelperMethods.HexStringToByteArray("F6E3C1C45DDCC44CB50355BC6EE1FD08"), HasChild=true },
                    new Folder { UserID=5, FolderName=HelperMethods.HexStringToByteArray("F6E3C1C45DDCC44CB50355BC6EE1FD08"), HasChild=true }
                };

                Folder[] sub_folds = new Folder[]
                {
                    new Folder { UserID=1, FolderName=HelperMethods.HexStringToByteArray("8C9628798BCB01B195B5343BE70E44DB"), HasChild=false, ParentID=5 },
                    new Folder { UserID=2, FolderName=HelperMethods.HexStringToByteArray("8C9628798BCB01B195B5343BE70E44DB"), HasChild=false, ParentID=4 },
                    new Folder { UserID=3, FolderName=HelperMethods.HexStringToByteArray("8C9628798BCB01B195B5343BE70E44DB"), HasChild=false, ParentID=3 },
                    new Folder { UserID=4, FolderName=HelperMethods.HexStringToByteArray("8C9628798BCB01B195B5343BE70E44DB"), HasChild=false, ParentID=2 },
                    new Folder { UserID=5, FolderName=HelperMethods.HexStringToByteArray("8C9628798BCB01B195B5343BE70E44DB"), HasChild=false, ParentID=1 }
                };

                foreach (Folder fold in base_folds) { context.Folders.Add(fold); } // add each account to the table
                context.SaveChanges();
                foreach (Folder fold in sub_folds) { context.Folders.Add(fold); } // add each account to the table
                context.SaveChanges(); // execute changes
            }
        }
    }
}
