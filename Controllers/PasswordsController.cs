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
        // generate a single password with the potential of applying regex standards in body
        [HttpGet("generate")]
        public string GeneratePassword()
        {
            return Generate();
        }

        //// generate a password based on specific allowed characters in regex format
        [HttpGet("generate/{regex}")]
        public string GeneratePassword(string regex)
        {
            /*
             * validate regex string here
             */

            return Generate(regex);
        }

        // private function to generate passwords based on allowed expression
        private string Generate(string regex = "[a-zA-Z0-9]")
        {
            return "value";
        }
    }
}
