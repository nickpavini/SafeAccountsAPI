using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Linq;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Newtonsoft.Json.Linq;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RefreshController : ControllerBase
    {

        private readonly APIContext _context; // database handle

        // get an instance of a database and http handle
        public RefreshController(APIContext context, IHttpContextAccessor httpContextAccessor)
        {
            _context = context;
        }


        // GET: api/RefreshToken
        [HttpPost]
        public string Refresh()
        {
            // attempt getting user from claims
            User user = HelperMethods.GetUserFromAccessToken(Request.Cookies["access_token"], _context);
            ValidateRefreshToken(user, Request.Cookies["refresh_token"]); // make sure this is a valid token for the user
            string newToken = HelperMethods.GenerateJWTAccessToken(user.Role, user.Email);
            RefreshToken newRefresh = HelperMethods.GenerateRefreshToken(user, _context);
            string ret = HelperMethods.GenerateLoginResponse(newToken, newRefresh, user.ID);
            _context.SaveChanges(); // save refresh token just before returning string to be safe

            CookieOptions options = new CookieOptions();
            options.HttpOnly = true;
            options.Secure = true;
            options.Expires = DateTime.UtcNow.AddDays(1); // set cookie to expire in 1 day

            Response.Cookies.Append("access_token", newToken, options); // boom this is it.. modify the response directly to include needed cookie
            Response.Cookies.Append("refresh_token", newRefresh.Token, options);
            return ret;
        }

        // make sure the refresh token is valid
        private void ValidateRefreshToken(User user, string refreshToken)
        {
            if (user == null || !user.RefreshTokens.Exists(rt => rt.Token == refreshToken))
            {
                throw new SecurityTokenException("Invalid token!");
            }

            var storedRefreshToken = user.RefreshTokens.Find(rt => rt.Token == refreshToken);

            // Ensure that the refresh token that we got from storage is not yet expired.
            if (DateTime.UtcNow > DateTime.Parse(storedRefreshToken.Expiration))
            {
                throw new SecurityTokenException("Invalid token!");
            }
        }
    }
}
