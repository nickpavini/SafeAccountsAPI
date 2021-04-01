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
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly APIContext _context;

        public AccountsController(APIContext context, IHttpContextAccessor httpContextAccessor) 
        {
            _httpContextAccessor = httpContextAccessor;
            _context = context;
        }

        // GET: api/Accounts
        [HttpGet] // in progress
        public string GetAllAccounts()
        {
            if (!HelperMethods.ValidateIsAdmin(_httpContextAccessor))
                return JObject.FromObject(new ErrorMessage("Invalid Role", "n/a", "Caller must have admin role.")).ToString(); // n/a for no args there

            return JObject.Parse(SuccessMessage._result).ToString();
        }
    }
}
