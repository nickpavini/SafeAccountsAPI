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
}
