using System;
using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Security.Claims;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Controllers
{
    [Route("[controller]")]
    [Authorize]
    [ApiController]
    public class AccountsController : ControllerBase
    {
        private bool isAdmin = false;
        public AccountsController(APIContext context, IHttpContextAccessor httpContextAccessor)
        {
            // all instances must be admin, users manage accounts throught the users API
            string callerRole = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role).Value;
            if (callerRole == UserRoles.Admin)
                isAdmin = true;
        }

        // GET: api/Accounts
        [HttpGet] // in progress
        public string GetAllAccounts()
        {
            if (!isAdmin)
                return JObject.FromObject(new ErrorMessage("Invalid Role", "n/a", "Caller must have admin role.")).ToString(); // n/a for no args there

            return JObject.Parse(SuccessMessage._result).ToString();
        }
    }
}
