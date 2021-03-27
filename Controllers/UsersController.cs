using System.Collections.Generic;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using Newtonsoft.Json.Linq;
using System;

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
        [HttpGet("{id:int}")]
        public User User_GetUser(int id)
        {
            return _context.Users.Where(a => a.ID == id).Single();
        }

        // POST /<controller>
        [HttpPost]
        public string User_AddUser([FromBody]string userJson)
        {
            JObject json = null;

            // might want Json verification as own function since all will do it.. we will see
            try { json = JObject.Parse(userJson); }
            catch (Exception ex) {
                ErrorMessage error = new ErrorMessage("Invalid Json", userJson, ex.Message);
                return JObject.FromObject(error).ToString();
            }

            return "";
        }

        [HttpGet("{id:int}/firstname")]
        public string User_GetFirstName(int id, [FromBody]string firstname)
        {
            return _context.Users.Where(a => a.ID == id).Single().First_Name;
        }

        [HttpPut("{id:int}/firstname")]
        public string User_EditFirstName(int id, [FromBody]string firstname)
        {
            try
            {
                _context.Users.Where(a => a.ID == id).Single().First_Name = firstname;
                _context.SaveChanges();
            }
            catch(Exception ex) {
                ErrorMessage error = new ErrorMessage("Failed to update first name.", "ID: "+id.ToString()+" First Name: "+firstname, ex.Message);
                return JObject.FromObject(error).ToString();
            }
            return @"{""result"":1}"; //result 1 or 0 if good
        }

        //// PUT /<controller>/5
        //[HttpPut("{username}")]
        //public string EditUser(string username, [FromBody]string userJson)
        //{
        //     JObject json = null;

        //    // might want Json verification as own function since all will do it.. we will see
        //    try { json = JObject.Parse(userJson); }
        //    catch (Exception ex) { return @"{""error"":""Invalid Json. Input: " + userJson + " Message: " + ex.ToString() + @"""}"; }

        //    return "";
        //}

        // DELETE api/<controller>/5
        [HttpDelete("{id:int}")]
        public void User_DeleteUser(int id)
        {
            _context.Users.Remove(_context.Users.Where(a => a.ID == id).Single());
            _context.SaveChanges();
        }

        // get all users accounts
        [HttpGet("{id:int}/accounts")]
        public IEnumerable<Account> User_GetAccounts(int id)
        {
            int user_id = _context.Users.Where(a => a.ID == id).Single().ID;
            return _context.Accounts.Where(a => a.UserID == user_id);
        }

        // add account.. input format is json
        [HttpPost("{id:int}/accounts")]
        public Account User_AddAccount([FromBody]string acc) 
        {
            return new Account();
        }
    }
}
