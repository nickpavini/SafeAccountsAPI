using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using SafeAccountsAPI.Models;
using Xunit;

namespace SafeAccountsAPI.UnitTests
{
    public class PasswordControllerIntegrationTest : IClassFixture<WebApplicationFactory<SafeAccountsAPI.Startup>>
    {
        public HttpClient _client { get; }
        public PasswordControllerIntegrationTest(WebApplicationFactory<SafeAccountsAPI.Startup> fixture)
        {
            _client = fixture.CreateClient();
        }

        [Fact]
        public async Task Post_Should_Generate_Password_With_Default_Options()
        {
            var passwordOptions = new PasswordOptions();
            var content = new StringContent(JsonConvert.SerializeObject(passwordOptions), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("passwords/generate", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task Post_Should_Generate_Password_With_Custom_Options()
        {
            var passwordOptions = new PasswordOptions() { MaxLength = 100, MinLength = 20 };
            var content = new StringContent(JsonConvert.SerializeObject(passwordOptions), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("passwords/generate", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
