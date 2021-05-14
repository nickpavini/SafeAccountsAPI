using System;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class RefreshController : ControllerBase
    {

        private readonly APIContext _context; // database handle
        public IConfiguration _configuration; //config handle

        // get an instance of a database and http handle
        public RefreshController(APIContext context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
        }


        // GET: api/RefreshToken
        [HttpPost]
        public IActionResult Refresh()
        {
            string rtrn; // return string as login response to send the user id and such
            try
            {
                // attempt getting user from claims
                User user = HelperMethods.GetUserFromAccessToken(Request.Cookies["AccessTokenSameSite"] ?? Request.Cookies["AccessToken"], _context, _configuration.GetValue<string>("JwtTokenKey"));
                ValidateRefreshToken(user, Request.Cookies["RefreshTokenSameSite"] ?? Request.Cookies["RefreshToken"]); // make sure this is a valid token for the user
                string newTokenStr = HelperMethods.GenerateJWTAccessToken(user.Role, user.Email, _configuration.GetValue<string>("JwtTokenKey"));
                RefreshToken newRefToken = HelperMethods.GenerateRefreshToken(user, _context);
                rtrn = HelperMethods.GenerateLoginResponse(newTokenStr, newRefToken, user.ID);
                _context.SaveChanges(); // save refresh token just before returning string to be safe

                // append cookies after refresh
                HelperMethods.SetCookies(Response, newTokenStr, newRefToken);
            }
            catch(Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error refreshing access.", ex.Message);
                return new InternalServerErrorResult(error);
            }

            return Ok(rtrn);
        }

        // make sure the refresh token is valid
        private void ValidateRefreshToken(User user, string refreshToken)
        {
            if (user == null || !user.RefreshTokens.Exists(rt => rt.Token == refreshToken))
            {
                throw new SecurityTokenException("Invalid token!");
            }

            RefreshToken storedRefreshToken = user.RefreshTokens.Find(rt => rt.Token == refreshToken);

            // Ensure that the refresh token that we got from storage is not yet expired.
            if (DateTime.UtcNow > DateTime.Parse(storedRefreshToken.Expiration))
            {
                throw new SecurityTokenException("Invalid token!");
            }
        }
    }
}
