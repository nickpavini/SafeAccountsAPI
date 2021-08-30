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
