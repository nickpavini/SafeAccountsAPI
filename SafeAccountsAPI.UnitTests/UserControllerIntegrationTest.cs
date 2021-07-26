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
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using SafeAccountsAPI.UnitTests.Helpers;
using Xunit;

namespace SafeAccountsAPI.UnitTests
{
    public class UserControllerIntegrationTest : IClassFixture<WebApplicationFactory<SafeAccountsAPI.Startup>>
    {
        public HttpClient _client { get; }
        public string _cookie { get; } // global cookie with a valid access and refresh token
        public IConfigurationRoot _config { get; }
        public APIContext _context { get; set; } // if we are updating things this might need to be disposed are reset
        User _testUser { get; set; } // this is our user for testing... also may be updated during testing
        ReturnableUser _retTestUser { get; set; } // decrypted user
        public string[] _keyAndIv { get; set; }

        public UserControllerIntegrationTest(WebApplicationFactory<SafeAccountsAPI.Startup> fixture)
        {
            _client = fixture.CreateClient();

            // get reference to app settings and local db
            _config = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
            DbContextOptions<APIContext> options = new DbContextOptions<APIContext>();
            _context = new APIContext(options, _config);

            // set default header for our api_key... Development key only, doesnt work with online api
            _client.DefaultRequestHeaders.Add("ApiKey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYXBpX2tleSIsImV4cCI6MTY1MzkxODQyNiwiaXNzIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAwIiwiYXVkIjoiaHR0cDovL2xvY2FsaG9zdDo1MDAwIn0.ZBagEGyp7dJBozJ7HoQ8nZVNpK-h-rzjXL9SmEvIYgA");

            // set reference to our user for testing
            _keyAndIv = new string[] { _config.GetValue<string>("UserEncryptionKey"), _config.GetValue<string>("UserEncryptionIV") }; // for user encryption there is a single key
            _testUser = _context.Users.Single(a => a.Email.SequenceEqual(HelperMethods.EncryptStringToBytes_Aes("john@doe.com", _keyAndIv))); // encrypted user
            _retTestUser = new ReturnableUser(_testUser, _keyAndIv); // decrypted user

            // generate access code and refresh token for use with endpoints that need to be logged in
            string accessToken = HelperMethods.GenerateJWTAccessToken(_testUser.ID, _config["UserJwtTokenKey"]);
            ReturnableRefreshToken refToken = new ReturnableRefreshToken(HelperMethods.GenerateRefreshToken(_testUser, _context, _keyAndIv), _keyAndIv);
            _cookie = "AccessToken=" + accessToken + "; AccessTokenSameSite=" + accessToken + "; RefreshToken=" + refToken.Token + "; RefreshTokenSameSite=" + refToken.Token;
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
                Login login = new Login { Email = _retTestUser.Email, Password = "useless" }; // the original user
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
                // add cookie, make request and validate status code
                requestMessage.Headers.Add("Cookie", _cookie);
                var response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // expected and returned users
                ReturnableUser returnedUser = JsonConvert.DeserializeObject<ReturnableUser>(response.Content.ReadAsStringAsync().Result);

                // check that the returned user is the user we were expecting.. checking for the correct hex strings
                Assert.Equal(_retTestUser.ID, returnedUser.ID);
                Assert.Equal(_retTestUser.Email, returnedUser.Email);
                Assert.Equal(_retTestUser.Role, returnedUser.Role);
                Assert.Equal(_retTestUser.First_Name, returnedUser.First_Name);
                Assert.Equal(_retTestUser.Last_Name, returnedUser.Last_Name);
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
                // Add cookie, make request and validate status code
                requestMessage.Headers.Add("Cookie", _cookie);
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

        [Fact]
        public async Task POST_AddNewAccount()
        {
            /*
             * HttpPost("users/{id}/accounts")
             * Add a new saved password account to the users data collection.
             */

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, _client.BaseAddress + "users/" + _testUser.ID.ToString() + "/accounts"))
            {
                // construct body with a new account to add..
                // we are using non encrypted hex strings for testing, but UI would send them encrypted
                NewAccount accToAdd = new NewAccount
                {
                    Title = "446973636f7264", // Discord
                    Login = "757365726e616d65", // username
                    Password = "7573656c657373", // useless
                    Url = "68747470733a2f2f646973636f72642e636f6d", // https://discord.com
                    Description = "6465736372697074696f6e2e2e2e" // description...
                };
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(accToAdd), Encoding.UTF8, "application/json");

                // Add cookie, make request and validate status code
                requestMessage.Headers.Add("Cookie", _cookie);
                HttpResponseMessage response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // parse account from response, and also request the data from the database directly for comparison
                ReturnableAccount returnedAcc = JsonConvert.DeserializeObject<ReturnableAccount>(response.Content.ReadAsStringAsync().Result);
                ReturnableAccount accInDatabase = new ReturnableAccount(_context.Accounts.SingleOrDefault(acc => acc.ID == returnedAcc.ID));

                // validate that the database had the account and that the data is equal
                Assert.NotNull(accInDatabase);
                Assert.Equal(returnedAcc.Title, accInDatabase.Title);
                Assert.Equal(returnedAcc.Login, accInDatabase.Login);
                Assert.Equal(returnedAcc.Password, accInDatabase.Password);
                Assert.Equal(returnedAcc.Url, accInDatabase.Url);
                Assert.Equal(returnedAcc.Description, accInDatabase.Description);
                Assert.Equal(returnedAcc.LastModified, accInDatabase.LastModified);
                Assert.Equal(returnedAcc.IsFavorite, accInDatabase.IsFavorite);

                // check for null folderid indicating no parent
                Assert.Null(returnedAcc.FolderID);
                Assert.Equal(returnedAcc.FolderID, accInDatabase.FolderID);
            }
        }
    }
}
