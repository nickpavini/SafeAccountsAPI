﻿using System;
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
        public string[] _keyAndIV; // this is the key that is used at the top level encryption

        // get an instance of a database and http handle
        public RefreshController(APIContext context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _context = context;
            _configuration = configuration;
            _keyAndIV = new string[] { _configuration.GetValue<string>("UserEncryptionKey"), _configuration.GetValue<string>("UserEncryptionIV") };
        }


        // GET: api/RefreshToken
        [HttpPost]
        [ApiExceptionFilter("Error refreshing access.")]
        public IActionResult Refresh()
        {
            // check for access token
            if (!Request.Headers.ContainsKey("AccessToken"))
            {
                ErrorMessage error = new ErrorMessage("Failed to refresh access", "User does not have the access token.");
                return new BadRequestObjectResult(error);
            }

            // check for refresh token
            if (!Request.Headers.ContainsKey("RefreshToken"))
            {
                ErrorMessage error = new ErrorMessage("Failed to refresh access", "User does not have the refresh token.");
                return new BadRequestObjectResult(error);
            }

            // attempt getting user from claims
            User user = HelperMethods.GetUserFromAccessToken(Request.Headers["AccessToken"].ToString(), _context, _configuration.GetValue<string>("UserJwtTokenKey"), _configuration.GetValue<string>("ApiUrl"));

            // make sure this is a valid token for the user
            if (!HelperMethods.ValidateRefreshToken(user, Request.Headers["RefreshToken"].ToString(), _keyAndIV))
            {
                ErrorMessage error = new ErrorMessage("Invalid refresh token", "Refrsh token could not be validated.");
                return new BadRequestObjectResult(error);
            }

            string newTokenStr = HelperMethods.GenerateJWTAccessToken(user.ID, _configuration.GetValue<string>("UserJwtTokenKey"), _configuration.GetValue<string>("ApiUrl"));
            RefreshToken newRefToken = HelperMethods.GenerateRefreshToken(user, _context, _keyAndIV);
            LoginResponse rtrn = new LoginResponse { ID = user.ID, AccessToken = newTokenStr, RefreshToken = new ReturnableRefreshToken(newRefToken, _keyAndIV) };

            return new OkObjectResult(rtrn);
        }
    }
}
