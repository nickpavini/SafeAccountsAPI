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
using SafeAccountsAPI.Controllers;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using SafeAccountsAPI.UnitTests.Helpers;
using Xunit;

namespace SafeAccountsAPI.UnitTests
{
    public class UserControllerIntegrationTest : IClassFixture<WebApplicationFactory<SafeAccountsAPI.Startup>>
    {
        public HttpClient _client { get; }
        public IConfigurationRoot _config { get; }
        public APIContext _context { get; set; } // if we are updating things this might need to be disposed are reset
        User _testUser { get; set; } // this is our user for testing... also may be updated during testing

        public UserControllerIntegrationTest(WebApplicationFactory<SafeAccountsAPI.Startup> fixture)
        {
            _client = fixture.CreateClient();

            // get reference to app settings and local db
            _config = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
            DbContextOptions<APIContext> options = new DbContextOptions<APIContext>();
            _context = new APIContext(options, _config);

            // set reference to our user for testing
            _testUser = _context.Users.Single(a => a.Email == "john@doe.com");
        }

        [Fact]
        public async Task Post_Should_Login_And_Return_Valid_Access_And_Refresh_Tokens()
        {
            /*
             * Login and use the newly recieved tokens to make api call and refresh.
             * Similar to refresh accept in the method we get our tokens. In refresh we generate 
             * our first tokens through code and strictly test the refresh endpoint, then validate the tokens recieved from refresh.
             * Here, we get our tokens from the login endpoint and make sure they work as expected.
             * NOTE: After this our _client variable has valid access tokens to make the rest of the tests as "john doe"
             * 
             */

            // make a login request and validate response code
            Login login = new Login { Email = _testUser.Email, Password = "useless" }; // the original user
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

        [Fact]
        public async Task Post_Should_Get_User_information()
        {
            /*
             * Get the user information and validate that what is returned is as expected.
             */

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, _client.BaseAddress + "users/" + _testUser.ID))
            {
                // generate access code and set header
                string accessToken = HelperMethods.GenerateJWTAccessToken(_testUser.Role, _testUser.Email, _config["JwtTokenKey"]);
                string cookie = "AccessToken=" + accessToken;
                requestMessage.Headers.Add("Cookie", cookie);

                // make request and validate status code
                var response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // expected and returned users
                ReturnableUser expectedUserReturn = new ReturnableUser(_testUser);
                ReturnableUser returnedUser = JsonConvert.DeserializeObject<ReturnableUser>(response.Content.ReadAsStringAsync().Result);

                // check that the returned user is the user we were expecting
                Assert.Equal(expectedUserReturn.ID, returnedUser.ID);
                Assert.Equal(expectedUserReturn.Email, returnedUser.Email);
                Assert.Equal(expectedUserReturn.Role, returnedUser.Role);
                Assert.Equal(expectedUserReturn.NumAccs, returnedUser.NumAccs);
                Assert.Equal(expectedUserReturn.First_Name, returnedUser.First_Name);
                Assert.Equal(expectedUserReturn.Last_Name, returnedUser.Last_Name);
            }
        }
    }
}
