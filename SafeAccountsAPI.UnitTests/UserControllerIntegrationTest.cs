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
        public string _accessToken { get; }
        public string _refreshToken { get; }

        public IConfigurationRoot _config { get; }
        public APIContext _context { get; set; } // if we are updating things this might need to be disposed are reset

        User _testUser { get; set; } // this is our user for testing... also may be updated during testing
        ReturnableUser _retTestUser { get; set; } // decrypted user

        // key and iv for encrypting/decrypting john doe's stored data based on his password of 'useless'
        public string[] _uniqueUserEncryptionKeyAndIv = new string[]
        {
            "MTZhNDFkOWNlMTE0ZGI5NjdiNGU0NGY1MGMwMGE4ODk=", // the is the password 'useless' sha256 encrypted then base 64 encoded to simulate client side encryption
            "MTIzNDU2Nzg5MDAwMDAwMA==" // client side encryption iv base 64 encoded
        };

        public string[] _keyAndIv { get; set; } // encryption key and iv used for all users base data,, this key cannot unluck user stored passwords/accounts

        public UserControllerIntegrationTest(WebApplicationFactory<SafeAccountsAPI.Startup> fixture)
        {
            _client = fixture.CreateClient();

            // get reference to app settings and local db
            _config = new ConfigurationBuilder().AddJsonFile("appsettings.Development.json").Build();
            DbContextOptions<APIContext> options = new DbContextOptions<APIContext>();
            _context = new APIContext(options, _config);

            // set default header for our api_key... Development key only, doesnt work with online api
            _client.DefaultRequestHeaders.Add("ApiKey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYXBpX2tleSIsImV4cCI6MTY2Mjk4NzA4MywiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzNjYiLCJhdWQiOiJodHRwczovL2xvY2FsaG9zdDo0NDM2NiJ9.NUf-fL3g72Z8XqihXJIuaG_z8_NEHmSwckb94VgVK3Q");

            // set reference to our user for testing
            _keyAndIv = new string[] { _config.GetValue<string>("UserEncryptionKey"), _config.GetValue<string>("UserEncryptionIV") }; // for user encryption there is a single key
            _testUser = _context.Users.Single(a => a.Email.SequenceEqual(HelperMethods.EncryptStringToBytes_Aes("john@doe.com", _keyAndIv))); // encrypted user
            _retTestUser = new ReturnableUser(_testUser, _keyAndIv); // decrypted user

            // generate access code and refresh token for use with endpoints that need to be logged in
            _accessToken = HelperMethods.GenerateJWTAccessToken(_testUser.ID, _config["UserJwtTokenKey"], _config.GetValue<string>("ApiUrl"));
            _refreshToken = new ReturnableRefreshToken(HelperMethods.GenerateRefreshToken(_testUser, _context, _keyAndIv), _keyAndIv).Token;
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

            // check that we recieved a valid login response
            JObject responseBody = await TestingHelpingMethods.CheckForLoginResponse(response);

            // make a call to the api to make sure we recieved a valid access token
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, _client.BaseAddress + "users/" + responseBody["id"].ToString()))
            {
                requestMessage.Headers.Add("AccessToken", responseBody["accessToken"].ToString()); // set new access and refresh tokens in cookies
                response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);
            }

            // make a call to refresh and check for 200 status code.. we dont need to validate refesh in anyway here
            using (var requestMessage = new HttpRequestMessage(HttpMethod.Get, _client.BaseAddress + "users/" + responseBody["id"].ToString()))
            {
                requestMessage.Headers.Add("AccessToken", responseBody["accessToken"].ToString()); // set new access and refresh tokens in Headers
                requestMessage.Headers.Add("RefreshToken", responseBody["refreshToken"]["token"].ToString()); // set new access and refresh tokens in Headers
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
                requestMessage.Headers.Add("AccessToken", _accessToken);
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
        public async Task POST_AddNewAccount()
        {
            /*
             * HttpPost("users/{id}/accounts")
             * Add a new saved password account to the users data collection.
             */

            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Post, _client.BaseAddress + "users/" + _testUser.ID.ToString() + "/accounts"))
            {
                // construct body with a new account to add.. using same techniques as client side encryption
                // so we send an encrypted account and receive an encrypted account
                NewAccount accToAdd = new NewAccount
                {
                    Title = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("Discord", _uniqueUserEncryptionKeyAndIv)).Replace("-", ""),
                    Login = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("username", _uniqueUserEncryptionKeyAndIv)).Replace("-", ""),
                    Password = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("useless", _uniqueUserEncryptionKeyAndIv)).Replace("-", ""),
                    Url = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("https://discord.com", _uniqueUserEncryptionKeyAndIv)).Replace("-", ""),
                    Description = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("description...", _uniqueUserEncryptionKeyAndIv)).Replace("-", "")
                };
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(accToAdd), Encoding.UTF8, "application/json");

                // Add cookie, make request and validate status code
                requestMessage.Headers.Add("AccessToken", _accessToken);
                HttpResponseMessage response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // parse account from response, and also request the data from the database directly for comparison
                ReturnableAccount returnedAcc = JsonConvert.DeserializeObject<ReturnableAccount>(response.Content.ReadAsStringAsync().Result);
                ReturnableAccount accInDatabase = new ReturnableAccount(_context.Accounts.SingleOrDefault(acc => acc.ID == returnedAcc.ID));
                TestingHelpingMethods.IntegrationTest_CompareAccounts(accToAdd, returnedAcc, accInDatabase); // make sure all are equal
                Assert.Null(returnedAcc.FolderID); // check for null folderid indicating no parent
            }
        }

        [Fact]
        public async Task PUT_EditAccount()
        {
            /*
             * HttpPut("users/{id}/accounts/{acc_id}")
             * Edits one of the users saved accounts in the database
             */

            int accId = 4; // user John Doe always has an account with id of 4 from the db initializer
            using (HttpRequestMessage requestMessage = new HttpRequestMessage(HttpMethod.Put, _client.BaseAddress + "users/" + _testUser.ID.ToString() + "/accounts/" + accId.ToString()))
            {
                // construct body with an encrypted account edit..
                // so we send an encrypted account and receive an encrypted account
                NewAccount accToEdit = new NewAccount
                {
                    Title = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("changed", _uniqueUserEncryptionKeyAndIv)).Replace("-", ""),
                    Login = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("changed", _uniqueUserEncryptionKeyAndIv)).Replace("-", ""),
                    Password = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("changed", _uniqueUserEncryptionKeyAndIv)).Replace("-", ""),
                    Url = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("https://changed.com", _uniqueUserEncryptionKeyAndIv)).Replace("-", ""),
                    Description = BitConverter.ToString(HelperMethods.EncryptStringToBytes_Aes("changed...", _uniqueUserEncryptionKeyAndIv)).Replace("-", "")
                };
                requestMessage.Content = new StringContent(JsonConvert.SerializeObject(accToEdit), Encoding.UTF8, "application/json");

                // Add cookie, make request and validate status code
                requestMessage.Headers.Add("AccessToken", _accessToken);
                HttpResponseMessage response = await _client.SendAsync(requestMessage);
                Assert.Equal(HttpStatusCode.OK, response.StatusCode);

                // parse account from response, and also request the data from the database directly for comparison
                ReturnableAccount returnedAcc = JsonConvert.DeserializeObject<ReturnableAccount>(response.Content.ReadAsStringAsync().Result);
                ReturnableAccount accInDatabase = new ReturnableAccount(_context.Accounts.SingleOrDefault(acc => acc.ID == returnedAcc.ID));
                TestingHelpingMethods.IntegrationTest_CompareAccounts(accToEdit, returnedAcc, accInDatabase); // make sure all are equal
            }
        }
    }
}
