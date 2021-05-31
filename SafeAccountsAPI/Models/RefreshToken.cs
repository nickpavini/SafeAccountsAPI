﻿namespace SafeAccountsAPI.Models
{
    public class RefreshToken
    {
        public int ID { get; set; }
        public int UserID { get; set; }
        public string Token { get; set; }
        public string Expiration { get; set; }
        public virtual User User { get; set; }
    }

    public class ReturnableRefreshToken
    {
        public string Token { get; set; }
        public string Expiration { get; set; }

        public ReturnableRefreshToken(RefreshToken rt)
        {
            Token = rt.Token;
            Expiration = rt.Expiration;
        }
    }
}
