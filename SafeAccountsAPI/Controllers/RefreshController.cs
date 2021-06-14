using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using Microsoft.Win32.SafeHandles;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Models;
using SafeAccountsAPI.Filters;

namespace SafeAccountsAPI.Controllers
{
    [Route("[controller]")]
    [Authorize(Policy = "ApiJwtToken")]
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
        [ApiExceptionFilter("Error refreshing access.")]
        public IActionResult Refresh()
        {
            // attempt getting user from claims
            User user = HelperMethods.GetUserFromAccessToken(Request.Cookies["AccessTokenSameSite"] ?? Request.Cookies["AccessToken"], _context, _configuration.GetValue<string>("UserJwtTokenKey"));

            // make sure this is a valid token for the user
            if (!HelperMethods.ValidateRefreshToken(user, Request.Cookies["RefreshTokenSameSite"] ?? Request.Cookies["RefreshToken"]))
            {
                ErrorMessage error = new ErrorMessage("Invalid refresh token", "Refrsh token could not be validated.");
                return new BadRequestObjectResult(error);
            }

            string newTokenStr = HelperMethods.GenerateJWTAccessToken(user.Role, user.Email, _configuration.GetValue<string>("UserJwtTokenKey"));
            RefreshToken newRefToken = HelperMethods.GenerateRefreshToken(user, _context);
            LoginResponse rtrn = new LoginResponse { ID = user.ID, AccessToken = newTokenStr, RefreshToken = new ReturnableRefreshToken(newRefToken) };

            // append cookies after refresh
            HelperMethods.SetCookies(Response, newTokenStr, newRefToken);
            return new OkObjectResult(rtrn);
        }
    }
}
