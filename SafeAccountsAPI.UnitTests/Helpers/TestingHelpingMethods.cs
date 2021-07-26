using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using System.Web;
using Newtonsoft.Json.Linq;
using Xunit;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.UnitTests.Helpers
{
    static class TestingHelpingMethods
    {
        // method for getting the set-cookie headers from a login or refresh response and validating they are the 4 as expected
        public static Dictionary<string, string> CheckForCookies(HttpResponseMessage response)
        {
            Assert.True(response.Headers.Contains("Set-Cookie"));
            Assert.True(response.Headers.GetValues("Set-Cookie").Count() == 4); // 4 cookies

            // get new cookie names
            Dictionary<string, string> new_cookies = new Dictionary<string, string>();
            foreach (string new_cookie in response.Headers.GetValues("Set-Cookie").ToList())
            {
                new_cookies.Add(new_cookie.Split(';')[0].Split('=')[0], HttpUtility.UrlDecode(new_cookie.Split(';')[0].Split('=')[1]));
            }

            // make sure the 4 cookies we recieved are as expected
            Assert.Contains("AccessToken", new_cookies.Keys);
            Assert.Contains("AccessTokenSameSite", new_cookies.Keys);
            Assert.Contains("RefreshTokenSameSite", new_cookies.Keys);
            Assert.Contains("RefreshToken", new_cookies.Keys);

            return new_cookies;
        }

        // used in login and refresh test to make sure that we recieved a valid login response
        public static async Task<JObject> CheckForLoginResponse(HttpResponseMessage response)
        {
            JObject responseBody = JObject.Parse(await response.Content.ReadAsStringAsync());
            Assert.NotNull(responseBody["id"]);
            Assert.NotNull(responseBody["accessToken"]);
            Assert.NotNull(responseBody["refreshToken"]);
            return responseBody;
        }

        // validate that the database editted the account and that the data is equal to what was returned, and what we sent
        public static void IntegrationTest_CompareAccounts(NewAccount sentData, ReturnableAccount retData, ReturnableAccount dataInDB)
        {
            Assert.NotNull(dataInDB);
            Assert.True(retData.Title == dataInDB.Title && retData.Title == sentData.Title);
            Assert.True(retData.Login == dataInDB.Login && retData.Login == sentData.Login);
            Assert.True(retData.Password == dataInDB.Password && retData.Password == sentData.Password);
            Assert.True(retData.Url == dataInDB.Url && retData.Url == sentData.Url);
            Assert.True(retData.Description == dataInDB.Description && retData.Description == sentData.Description);
            Assert.Equal(retData.LastModified, dataInDB.LastModified);
            Assert.Equal(retData.IsFavorite, dataInDB.IsFavorite);
            Assert.Equal(retData.FolderID, dataInDB.FolderID);
        }
    }
}
