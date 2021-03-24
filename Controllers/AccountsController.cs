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
    public class AccountsController : ControllerBase
    {
        // GET: api/Accounts
        [HttpGet]
        public IEnumerable<string> GetAllAccounts()
        {
            return new string[] { "value1", "value2" };
        }

        // GET: api/Accounts/5
        // get account by primary key
        [HttpGet("{id:int}")]
        public string GetAccount(int id)
        {
            return "value";
        }

        // POST: api/Accounts
        [HttpPost]
        public void AddAccount([FromBody] string value)
        {
        }

        // PUT: api/Accounts/5
        [HttpPut("{id}")]
        public void EditAccount(int id, [FromBody] string value)
        {
        }

        // DELETE: api/ApiWithActions/5
        [HttpDelete("{id}")]
        public void DeleteAccuont(int id)
        {
        }
    }
}
