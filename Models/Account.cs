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
            Password = HelperMethods.DecryptStringFromBytes_Aes(acc.Password, HelperMethods.temp_password_key); // this later will nees to be editted for logic to decrypt based on each users key
            Description = acc.Description;

            if (acc.FolderID != null)
                FolderID = acc.FolderID;
        }
    }
}
