using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using SafeAccountsAPI.UnitTests.Helpers;
using Xunit;
using System.IO;

namespace SafeAccountsAPI.UnitTests
{
    public class RefreshControllerIntegrationTest : IClassFixture<WebApplicationFactory<SafeAccountsAPI.Startup>>
    {
        public HttpClient _client { get; }
        public RefreshControllerIntegrationTest(WebApplicationFactory<SafeAccountsAPI.Startup> fixture)
        {
            _client = fixture.CreateClient();

            // set default header for our api_key... Development key only, doesnt work with online api
            _client.DefaultRequestHeaders.Add("ApiKey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYXBpX2tleSIsImV4cCI6MTY2Mjk4NzA4MywiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzNjYiLCJhdWQiOiJodHRwczovL2xvY2FsaG9zdDo0NDM2NiJ9.NUf-fL3g72Z8XqihXJIuaG_z8_NEHmSwckb94VgVK3Q");
        }

        [Fact]
        public async Task POST_Should_Return_New_Valid_Cookies()
        {
            /*
             * HttpPost("refresh")
             * For this test we need a valid refresh token, and an access token that is expired or not.
             * Then make a call to refresh and use the received tokens to see if we can make a valid call to the api.
             * And finally we make a call to validateRefreshToken to make sure the newly generated refresh was stored in the DB
             */

            // get reference to app settings and local db
            var config = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
            DbContextOptions<APIContext> options = new DbContextOptions<APIContext>();
            APIContext context = new APIContext(options, config);

            // generate an access token and valid refresh token
            string[] keyAndIV = { config.GetValue<string>("UserEncryptionKey"), config.GetValue<string>("UserEncryptionIV") }; // for user encryption there is a single key
            User user = context.Users.Single(a => a.Email.SequenceEqual(HelperMethods.EncryptStringToBytes_Aes("john@doe.com", keyAndIV)));
            string accessToken = HelperMethods.GenerateJWTAccessToken(user.ID, config["UserJwtTokenKey"], config.GetValue<string>("ApiUrl"));
            ReturnableRefreshToken refToken = new ReturnableRefreshToken(HelperMethods.GenerateRefreshToken(user, context, keyAndIV), keyAndIV);

            // set access and refresh in header
            _client.DefaultRequestHeaders.Add("AccessToken", accessToken);
            _client.DefaultRequestHeaders.Add("RefreshToken", refToken.Token);

            // send request to refresh and assert request is ok and new cookies are present
            var response = await _client.PostAsync("refresh", null);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // check that we recieved a valid login response
            JObject responseBody = await TestingHelpingMethods.CheckForLoginResponse(response);

            // set new access token in cookies
            _client.DefaultRequestHeaders.Remove("AccessToken");
            _client.DefaultRequestHeaders.Add("AccessToken", responseBody["accessToken"].ToString());

            // make a call to the api to make sure we recieved a valid access token
            response = await _client.GetAsync("users/" + responseBody["id"].ToString());
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);

            // and the last thing we need is to validate that the refresh token was stored in the DB
            context.Dispose(); // close old connection
            user = new APIContext(options, config).Users.Single(a => a.Email.SequenceEqual(HelperMethods.EncryptStringToBytes_Aes("john@doe.com", keyAndIV))); // get fresh handle of user from the DB
            Assert.True(HelperMethods.ValidateRefreshToken(user, responseBody["refreshToken"]["token"].ToString(), keyAndIV));
        }
    }
}
