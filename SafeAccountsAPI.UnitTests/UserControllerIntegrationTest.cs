using System;
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
        public async Task POST_Should_Login_And_Return_Valid_Access_And_Refresh_Tokens()
        {
            /*
             * HttpPost("users/login")
             * Login and use the newly recieved tokens to make api call and refresh.
             * Similar to refresh accept in the method we get our tokens. In refresh we generate 
             * our first tokens through code and strictly test the refresh endpoint, then validate the tokens recieved from refresh.
             * Here, we get our tokens from the login endpoint and make sure they work as expected.
             * 
             */

            // variable for managing our responses
            HttpResponseMessage response;

            // make a login request and validate response code
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _client.BaseAddress + "users/login"))
            {
                Login login = new Login { Email = _testUser.Email, Password = "useless" }; // the original user
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(login), Encoding.UTF8, "application/json"); // set content
                response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // valid cookies presence, retrieve, and create a cookie header string
            Dictionary<string, string> new_cookies = TestingHelpingMethods.CheckForCookies(response);
            string cookie = "AccessTokenSameSite=" + new_cookies.Single(a => a.Key == "AccessTokenSameSite").Value
                                    + "; RefreshTokenSameSite=" + new_cookies.Single(a => a.Key == "RefreshTokenSameSite").Value;

            // check that we recieved a valid login response
            JObject responseBody = await TestingHelpingMethods.CheckForLoginResponse(response);

            // make a call to the api to make sure we recieved a valid access token
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, _client.BaseAddress + "users/" + responseBody["id"].ToString()))
            {
                requestMessage.Headers.Add("Cookie", cookie); // set new access and refresh tokens in cookies
                response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // make a call to refresh and check for 200 status code.. we dont need to validate refesh in anyway here
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, _client.BaseAddress + "users/" + responseBody["id"].ToString()))
            {
                requestMessage.Headers.Add("Cookie", cookie); // set new access and refresh tokens in cookies
                response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }
        }

        [Fact]
        public async Task GET_Should_Retrieve_User_information()
        {
            /*
             * HttpGet("users/{id}")
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

        [Fact]
        public async Task POST_SignOut_Should_Return_Expired_Set_Cookie_Headers()
        {
            /*
             * HttpPost("users/logout")
             * Signout and check that we got 4 empty and expired set cookie headers
             */

            using (var requestMessage = new HttpRequestMessage(HttpMethod.Post, _client.BaseAddress + "users/logout"))
            {
                // generate access code and set header
                string accessToken = HelperMethods.GenerateJWTAccessToken(_testUser.Role, _testUser.Email, _config["JwtTokenKey"]);
                RefreshToken refToken = HelperMethods.GenerateRefreshToken(_testUser, _context);
                string cookie = "AccessToken=" + accessToken + "; AccessTokenSameSite=" + accessToken + "; RefreshToken=" + refToken.Token + "; RefreshTokenSameSite=" + refToken.Token;
                requestMessage.Headers.Add("Cookie", cookie);

                // make request and validate status code
                var response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // make sure cookies exist, and then make sure all are expired and empty
                Dictionary<string, string> cookiesToDelete = TestingHelpingMethods.CheckForCookies(response);
                foreach (string delete_cookie in response.Headers.GetValues("Set-Cookie").ToList())
                {
                    string date = delete_cookie.Split(';')[1].Split('=')[1];
                    DateTime expiringDate = DateTime.Parse(date);
                    Assert.True(DateTime.Now > expiringDate); // make sure expired
                    Assert.Equal("", cookiesToDelete[delete_cookie.Split(';')[0].Split('=')[0]]); // make sure each is empty
                }
            }
        }
    }
}
