using SafeAccountsAPI.Helpers;

namespace SafeAccountsAPI.Models
{
    public class RefreshToken
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public byte[] Token { get; set; }
        public byte[] Expiration { get; set; }
        public virtual User User { get; set; }
    }

    public class ReturnableRefreshToken
    {
        public string Token { get; set; }
        public string Expiration { get; set; }

        public ReturnableRefreshToken(RefreshToken rt, string[] keyAndIv)
        {
            Token = HelperMethods.DecryptStringFromBytes_Aes(rt.Token, keyAndIv);
            Expiration = HelperMethods.DecryptStringFromBytes_Aes(rt.Expiration, keyAndIv);
        }
    }
}
