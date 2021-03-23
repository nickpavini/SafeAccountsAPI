using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;

namespace SafeAccountsAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PasswordsController : ControllerBase
    {
        // GET: api/Passwords
        // generate a single password
        [HttpGet("generate")]
        public IEnumerable<string> GeneratePassword()
        {
            return new string[] { "value1", "value2" };
        }
    }
}
