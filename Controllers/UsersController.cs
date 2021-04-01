using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Security.Claims;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SafeAccountsAPI.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class UsersController : Controller
    {
        private readonly APIContext _context; // database handle
        private readonly IHttpContextAccessor _httpContextAccessor = null; // handle to all http information.. used for authorization

        // get an instance of a database and http handle
        public UsersController(APIContext context, IHttpContextAccessor httpContextAccessor) { 
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        [HttpPost("login")]
        public string User_Login([FromBody]string credentials)
        {
            JObject json = null;
            try { json = JObject.Parse(credentials); }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Invalid Json", credentials, ex.Message);
                return JObject.FromObject(error).ToString();
            }

            try
            {
                User user = _context.Users.Single(a => a.Email == json["email"].ToString());
                string userPass = user.Password;

                // successful login
                if (userPass == json["password"].ToString())
                {
                    var tokenString = HelperMethods.GenerateJWTAccessToken(user.Role, user.Email);
                    RefreshToken refToken = HelperMethods.GenerateRefreshToken(user, _context);
                    return HelperMethods.GenerateLoginResponse(tokenString, refToken, user.ID);
                }
                else
                {
                    ErrorMessage error = new ErrorMessage("Invalid Credentials", credentials, Unauthorized().ToString());
                    return JObject.FromObject(error).ToString();
                }
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error validating credentials", credentials, ex.Message);
                return JObject.FromObject(error).ToString();
            }
        }

        // GET: /<controller>
        // Get all available users.. might change later as it might not make sense to grab all accounts if there are tons
        // More of an admin functionality
        [HttpGet, Authorize]
        public string GetAllUsers()
        {
            string callerRole = _httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role).Value;
            if (callerRole != UserRoles.Admin)
                return JObject.FromObject(new ErrorMessage("Invalid Role", "Caller's Role: " + callerRole, "Caller must have admin role.")).ToString();

            // format success response.. maybe could be done better but not sure yet
            JObject message = JObject.Parse(SuccessMessage._result);
            JArray users = new JArray();
            foreach (User user in _context.Users.ToArray()) {
               ReturnableUser retUser = new ReturnableUser(user);
               users.Add(JToken.FromObject(retUser));
            }
            message.Add(new JProperty("users", users));
            return message.ToString();
        }

        // GET /<controller>/5
        // Get a specific user.. later we will need to learn about the authentications and such
        [HttpGet("{id:int}"), Authorize]
        public string User_GetUser(int id)
        {
            // Get email from the token and compare it with the email of the user they are trying to access
            ClaimsPrincipal claims = _httpContextAccessor.HttpContext.User;
            string callerEmail = claims.FindFirst(ClaimTypes.Email).Value;
            string callerRole = claims.FindFirst(ClaimTypes.Role).Value;

            // verify that the user is either admin or is requesting their own data
            if (callerEmail != _context.Users.Single(a => a.ID == id).Email && callerRole != UserRoles.Admin)
                return JObject.FromObject(new ErrorMessage("Invalid User", "Caller's Email: " + callerEmail + " Caller's Role: " + callerRole, "Caller can only access their information.")).ToString();

            //format response
            JObject message = JObject.Parse(SuccessMessage._result);
            ReturnableUser retUser = new ReturnableUser(_context.Users.Where(a => a.ID == id).Single()); // strips out private data that is never to be sent back
            message.Add(new JProperty("user", JToken.FromObject(retUser)));
            return message.ToString();
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
        public string User_GetFirstName(int id)
        {
            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("firstname", _context.Users.Where(a => a.ID == id).Single().First_Name));
            return message.ToString();
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

            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("new_firstname", _context.Users.Where(a => a.ID == id).Single().First_Name)); // this part re-affirms that in the database we have a new firstname
            return message.ToString();
        }

        [HttpGet("{id:int}/lastname")]
        public string User_GetLastName(int id)
        {
            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("lastname", _context.Users.Where(a => a.ID == id).Single().Last_Name));
            return message.ToString();
        }

        [HttpPut("{id:int}/lastname")]
        public string User_EditLastName(int id, [FromBody]string lastname)
        {
            try
            {
                _context.Users.Where(a => a.ID == id).Single().Last_Name = lastname;
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to update last name.", "ID: " + id.ToString() + " Last Name: " + lastname, ex.Message);
                return JObject.FromObject(error).ToString();
            }

            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("new_lastname", _context.Users.Where(a => a.ID == id).Single().Last_Name)); // this part re-affirms that in the database we have a new firstname
            return message.ToString();
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
        public string User_DeleteUser(int id)
        {
            try
            {
                _context.Users.Remove(_context.Users.Single(a => a.ID == id));
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to delete user.", "ID: " + id.ToString(), ex.Message);
                return JObject.FromObject(error).ToString();
            }

            JObject message = JObject.Parse(SuccessMessage._result);
            return message.ToString();
        }

        // get all users accounts
        [HttpGet("{id:int}/accounts"), Authorize]
        public string User_GetAccounts(int id)
        {
            // Get email from the token and compare it with the email of the user they are trying to access
            ClaimsPrincipal claims = _httpContextAccessor.HttpContext.User;
            string callerEmail = claims.FindFirst(ClaimTypes.Email).Value;
            string callerRole = claims.FindFirst(ClaimTypes.Role).Value;

            // verify that the user is either admin or is requesting their own data
            if (callerEmail != _context.Users.Single(a => a.ID == id).Email && callerRole != UserRoles.Admin)
                return JObject.FromObject(new ErrorMessage("Invalid User", "Caller's Email: " + callerEmail + " Caller's Role: " + callerRole, "Caller can only access their information.")).ToString();

            // format success response.. maybe could be done better but not sure yet
            JObject message = JObject.Parse(SuccessMessage._result);
            //message.Add(new JProperty("user", JToken.FromObject(new ReturnableUser(_context.Users.Single(a => a.ID == id)))));
            JArray accs = new JArray();
            foreach (Account acc in _context.Users.Single(a => a.ID == id).Accounts)
            {
                accs.Add(JToken.FromObject(new ReturnableAccount(acc)));
            }
            message.Add(new JProperty("accounts", accs));
            return message.ToString();
        }

        // add account.. input format is json
        [HttpPost("{id:int}/accounts")]
        public string User_AddAccount(int id, [FromBody]string accJson) 
        {
            return "";
        }
    }
}
