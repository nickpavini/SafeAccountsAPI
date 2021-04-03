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
        public string Refresh([FromBody]string request)
        {
            // parse json.. might also want a function, maybe a helper methods file for all code minimization needs
            JObject json = null;
            try { json = JObject.Parse(request); }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Invalid Json", request, ex.Message);
                return JObject.FromObject(error).ToString();
            }

            // attempt getting user from claims
            User user = HelperMethods.GetUserFromAccessToken(json["access_token"].ToString(), _context);
            ValidateRefreshToken(user, json["refresh_token"].ToString()); // make sure this is a valid token for the user
            string newToken = HelperMethods.GenerateJWTAccessToken(user.Role, user.Email);
            RefreshToken newRefresh = HelperMethods.GenerateRefreshToken(user, _context);
            string ret = HelperMethods.GenerateLoginResponse(newToken, newRefresh, user.ID);
            _context.SaveChanges(); // save refresh token just before returning string to be safe
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
