using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json;
using Newtonsoft.Json.Linq;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using SafeAccountsAPI.UnitTests.Helpers;
using Xunit;

namespace SafeAccountsAPI.UnitTests
{
    public class UserControllerIntegrationTest : IClassFixture<WebApplicationFactory<SafeAccountsAPI.Startup>>
    {
        public HttpClient _client { get; }
        public UserControllerIntegrationTest(WebApplicationFactory<SafeAccountsAPI.Startup> fixture)
        {
            _client = fixture.CreateClient();
        }

        [Fact]
        public async Task Post_Should_Login_And_Return_Valid_Access_And_Refresh_Tokens()
        {
            /*
             * Login and use the newly recieved tokens to make api call and refresh.
             * Similar to refresh accept in the method we get our tokens. In refresh we generate 
             * our first tokens through code and strictly test the refresh endpoint, then validate the tokens recieved from refresh.
             * Here, we get our tokens from the login endpoint and make sure they work as expected.
             */

            // make a login request and validate response code
            Login login = new Login { Email = "john@doe.com", Password = "useless" }; // the original user
            StringContent content = new StringContent(JsonConvert.SerializeObject(login), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("users/login", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode); // make sure result is good

            // valid cookies presence and retrieve
            Dictionary<string, string> new_cookies = TestingHelpingMethods.CheckForCookies(response);

            // check that we recieved a valid login response
            JObject responseBody = await TestingHelpingMethods.CheckForLoginResponse(response);

            // set new access token in cookies
            string cookie = "AccessTokenSameSite=" + new_cookies.Single(a => a.Key == "AccessTokenSameSite").Value
                                + "; RefreshTokenSameSite=" + new_cookies.Single(a => a.Key == "RefreshTokenSameSite").Value;
            _client.DefaultRequestHeaders.Add("Cookie", cookie);

            // make a call to the api to make sure we recieved a valid access token
            response = await _client.GetAsync("users/" + responseBody["id"].ToString());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // make a call to refresh and check for 200 status code.. we dont need to validate refesh in anyway here
            response = await _client.PostAsync("refresh", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
