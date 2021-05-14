using Newtonsoft.Json;
using SafeAccountsAPI.Controllers;
using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations.Schema;
using System.Linq;
using System.Threading.Tasks;

namespace SafeAccountsAPI.Models
{
    public class Account
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public virtual User User { get; set; }
        public int? FolderID { get; set; }
        public virtual Folder Folder { get; set; }
        public string Title { get; set; }
        public string Login { get; set; }
        public byte[] Password { get; set; }
        public string Description { get; set; }

        public Account() { } // blank constructor needed for db initializer

        // constructor to easily set from NewUser type
        public Account(NewAccount newAcc, int uid)
        {
            UserID = uid;
            Title = newAcc.Title;
            Login = newAcc.Login;
            Password = HelperMethods.EncryptStringToBytes_Aes(newAcc.Password, HelperMethods.GetUserKeyAndIV(uid));
            Description = newAcc.Description;
            FolderID = newAcc.FolderID;
        }
    }

    public class ReturnableAccount
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Description { get; set; }
        public int? FolderID { get; set; }

        public ReturnableAccount(Account acc)
        {
            ID = acc.ID;
            Title = acc.Title;
            Login = acc.Login;
            Password = HelperMethods.DecryptStringFromBytes_Aes(acc.Password, HelperMethods.GetUserKeyAndIV(acc.UserID));
            Description = acc.Description;

            if (acc.FolderID != null)
                FolderID = acc.FolderID;
        }
    }

    public class NewAccount
    {
        [JsonProperty]
        public string Title { get; set; }
        [JsonProperty]
        public string Login { get; set; }
        [JsonProperty]
        public string Password { get; set; }
        [JsonProperty]
        public string Description { get; set; }
        [JsonProperty]
        public int? FolderID { get; set; }
    }
}
