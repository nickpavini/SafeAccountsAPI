using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.EntityFrameworkCore;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SafeAccountsAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : Controller
    {
        private readonly APIContext _context; // database handle

        public UsersController(APIContext context) { _context = context; } // get an instance of a database handle

        // GET: /<controller>
        // Get all available users.. might change later as it might not make sense to grab all accounts if there are tons
        [HttpGet]
        public IEnumerable<User> GetAllUsers()
        {
            return _context.Users.ToArray();
        }

        // GET /<controller>/5
        // Get a specific user.. later we will need to learn about the authentications and such
        [HttpGet("{username}")]
        public User GetUser(string username)
        {
            return _context.Users.Where(a => a.User_Name == username).Single();
            //return new User();
        }

        // POST /<controller>
        [HttpPost]
        public void AddUser([FromBody]string value)
        {
        }

        // PUT /<controller>/5
        [HttpPut("{username}")]
        public void EditUser(string username, [FromBody]string value)
        {
        }

        // DELETE api/<controller>/5
        [HttpDelete("{username}")]
        public void DeleteUser(string username)
        {
            _context.Users.Remove(_context.Users.Where(a => a.User_Name == username).Single());
            _context.SaveChanges();
        }

        [HttpGet("{username}/firstname")]
        public string User_GetFirstName(string username)
        {
            return _context.Users.Where(a => a.User_Name == username).Single().First_Name;
        }
    }
}
