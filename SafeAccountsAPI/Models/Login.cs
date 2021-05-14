using Newtonsoft.Json;

namespace SafeAccountsAPI.Models
{
    public class Login
    {
        [JsonProperty(PropertyName = "email")]
        public string Email { get; set; }
        [JsonProperty(PropertyName = "password")]
        public string Password { get; set; }
    }

    // login response with valid access token, refresh token, and the id for user navigation
    public class LoginResponse
    {
        public int ID { get; set; }
        public string AccessToken { get; set; }
        public ReturnableRefreshToken RefreshToken { get; set; }
    }
}
