using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Web;
using Xunit;

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
    }
}
