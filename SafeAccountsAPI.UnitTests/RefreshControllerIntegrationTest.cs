using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using SafeAccountsAPI.Controllers;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using SafeAccountsAPI.UnitTests.Helpers;
using Xunit;

namespace SafeAccountsAPI.UnitTests
{
    public class RefreshControllerIntegrationTest : IClassFixture<WebApplicationFactory<SafeAccountsAPI.Startup>>
    {
        public HttpClient _client { get; }
        public RefreshControllerIntegrationTest(WebApplicationFactory<SafeAccountsAPI.Startup> fixture)
        {
            _client = fixture.CreateClient();
        }

        [Fact]
        public async Task Post_Should_Return_New_Valid_Cookies()
        {
            /*
             * For this test we need a valid refresh token, and an access token that is expired or not.
             * Then make a call to refresh and use the received tokens to see if we can make a valid call to the api.
             * And finally we make a call to validateRefreshToken to make sure the newly generated refresh was stored in the DB
             */

            // get reference to app settings and local db
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
            DbContextOptions<APIContext> options = new DbContextOptions<APIContext>();
            APIContext context = new APIContext(options, config);

            // generate an access token and valid refresh token
            User user = context.Users.Single(a => a.Email == "john@doe.com");
            string accessToken = HelperMethods.GenerateJWTAccessToken(user.Role, user.Email, config["JwtTokenKey"]);
            RefreshToken refToken = HelperMethods.GenerateRefreshToken(user, context);

            // set cookies in header
            string cookie = "AccessToken=" + accessToken + "; RefreshToken=" + refToken.Token;
            _client.DefaultRequestHeaders.Add("Cookie", cookie);

            // send request to refresh and assert request is ok and new cookies are present
            var response = await _client.PostAsync("refresh", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // valid cookies presence and retrieve
            Dictionary<string, string> new_cookies = TestingHelpingMethods.CheckForCookies(response);

            // set new access token in cookies
            _client.DefaultRequestHeaders.Remove("Cookie");
            cookie = "AccessTokenSameSite=" + new_cookies.Single(a => a.Key == "AccessTokenSameSite").Value;
            _client.DefaultRequestHeaders.Add("Cookie", cookie);

            // make a call to the api to make sure we recieved a valid access token
            response = await _client.GetAsync("users/" + user.ID.ToString());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // and the last thing we need is to validate that the refresh token was stored in the DB
            context.Dispose(); // close old connection
            user = new APIContext(options, config).Users.Single(a => a.Email == "john@doe.com"); // get fresh handle of user from the DB
            Assert.True(HelperMethods.ValidateRefreshToken(user, new_cookies.Single(a => a.Key == "RefreshTokenSameSite").Value));
        }
    }
}
