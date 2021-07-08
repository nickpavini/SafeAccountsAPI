using System;
using Newtonsoft.Json;
using SafeAccountsAPI.Helpers;

namespace SafeAccountsAPI.Models
{
    public class Account
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public virtual User User { get; set; }
        public int? FolderID { get; set; }
        public virtual Folder Folder { get; set; }
        public byte[] Title { get; set; }
        public byte[] Login { get; set; }
        public byte[] Password { get; set; }
        public byte[] Url { get; set; }
        public byte[] Description { get; set; }
        public string LastModified { get; set; }
        public bool IsFavorite { get; set; }

        public Account() { } // blank constructor needed for db initializer

        // constructor to easily set from NewUser type
        public Account(NewAccount newAcc, int uid)
        {
            UserID = uid;
            Title = HelperMethods.HexStringToByteArray(newAcc.Title);
            Login = HelperMethods.HexStringToByteArray(newAcc.Login);
            Password = HelperMethods.HexStringToByteArray(newAcc.Password);
            Url = HelperMethods.HexStringToByteArray(newAcc.Url);
            Description = HelperMethods.HexStringToByteArray(newAcc.Description);
            FolderID = newAcc.FolderID;
            IsFavorite = false;
        }
    }

    public class ReturnableAccount
    {
        public int ID { get; set; }
        public string Title { get; set; }
        public string Login { get; set; }
        public string Password { get; set; }
        public string Url { get; set; }
        public string Description { get; set; }
        public string LastModified { get; set; }
        public int? FolderID { get; set; }
        public bool IsFavorite { get; set; }

        public ReturnableAccount(Account acc)
        {
            ID = acc.ID;
            Title = BitConverter.ToString(acc.Title).Replace("-", "");
            Login = BitConverter.ToString(acc.Login).Replace("-", "");
            Password = BitConverter.ToString(acc.Password).Replace("-", "");
            Url = BitConverter.ToString(acc.Url).Replace("-", "");
            Description = BitConverter.ToString(acc.Description).Replace("-", "");
            LastModified = acc.LastModified;
            IsFavorite = acc.IsFavorite;

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
        public string Url { get; set; }
        [JsonProperty]
        public string Description { get; set; }
        [JsonProperty]
        public int? FolderID { get; set; }
    }
}
