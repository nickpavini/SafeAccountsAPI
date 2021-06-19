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
        public byte[] LastModified { get; set; }
        public bool IsFavorite { get; set; }

        public Account() { } // blank constructor needed for db initializer

        // constructor to easily set from NewUser type
        public Account(NewAccount newAcc, int uid)
        {
            UserID = uid;
            Title = HelperMethods.EncryptStringToBytes_Aes(newAcc.Title, HelperMethods.GetUserKeyAndIV(uid));
            Login = HelperMethods.EncryptStringToBytes_Aes(newAcc.Login, HelperMethods.GetUserKeyAndIV(uid));
            Password = HelperMethods.EncryptStringToBytes_Aes(newAcc.Password, HelperMethods.GetUserKeyAndIV(uid));
            Url = HelperMethods.EncryptStringToBytes_Aes(newAcc.Url, HelperMethods.GetUserKeyAndIV(uid));
            Description = HelperMethods.EncryptStringToBytes_Aes(newAcc.Description, HelperMethods.GetUserKeyAndIV(uid));
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
            Title = HelperMethods.DecryptStringFromBytes_Aes(acc.Title, HelperMethods.GetUserKeyAndIV(acc.UserID));
            Login = HelperMethods.DecryptStringFromBytes_Aes(acc.Login, HelperMethods.GetUserKeyAndIV(acc.UserID));
            Password = HelperMethods.DecryptStringFromBytes_Aes(acc.Password, HelperMethods.GetUserKeyAndIV(acc.UserID));
            Url = HelperMethods.DecryptStringFromBytes_Aes(acc.Url, HelperMethods.GetUserKeyAndIV(acc.UserID));
            Description = HelperMethods.DecryptStringFromBytes_Aes(acc.Description, HelperMethods.GetUserKeyAndIV(acc.UserID));
            LastModified = HelperMethods.DecryptStringFromBytes_Aes(acc.LastModified, HelperMethods.GetUserKeyAndIV(acc.UserID));
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
