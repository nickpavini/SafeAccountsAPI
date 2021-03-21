using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using SafeAccountsAPI.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SafeAccountsAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : Controller
    {
        // GET: api/<controller>
        [HttpGet]
        public IEnumerable<User> Get()
        {
            return Enumerable.Range(1, 5).Select(index => new User
            {
                name = "Bob",
                age = 37,
                gender = "male"
            })
           .ToArray();
        }

        // GET api/<controller>/5
        [HttpGet("{id}")]
        public User Get(int id)
        {
            return new User();
        }

        // POST api/<controller>
        [HttpPost]
        public void Post([FromBody]string value)
        {
        }

        // PUT api/<controller>/5
        [HttpPut("{id}")]
        public void Put(int id, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        [HttpDelete("{id}")]
        public void Delete(int id)
        {
        }
    }
}
