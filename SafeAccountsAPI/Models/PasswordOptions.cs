using Newtonsoft.Json;

namespace SafeAccountsAPI.Models
{
    public class PasswordOptions
    {
        [JsonProperty]
        public int MinLength { get; set; } = 8;
        [JsonProperty]
        public int MaxLength { get; set; } = 12;
        [JsonProperty]
        public string RegexPattern { get; set; } = "[a-zA-Z0-9]";
    }
}
