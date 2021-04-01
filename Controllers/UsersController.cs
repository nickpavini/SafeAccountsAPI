using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using Newtonsoft.Json.Linq;
using System;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
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

        // login and get tokens...
        [HttpPost("login")] //working
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
                    string ret = HelperMethods.GenerateLoginResponse(tokenString, refToken, user.ID);
                    _context.SaveChanges(); // always last on db to make sure nothing breaks and db has new info
                    return ret;
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

        // Get all available users.. might change later as it might not make sense to grab all accounts if there are tons
        [HttpGet, Authorize] //working
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

        // Get a specific user.
        [HttpGet("{id:int}"), Authorize] // working
        public string User_GetUser(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

            //format response
            JObject message = JObject.Parse(SuccessMessage._result);
            ReturnableUser retUser = new ReturnableUser(_context.Users.Where(a => a.ID == id).Single()); // strips out private data that is never to be sent back
            message.Add(new JProperty("user", JToken.FromObject(retUser)));
            return message.ToString();
        }

        // register new user
        [HttpPost] // in progress
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

        [HttpGet("{id:int}/firstname"), Authorize] // working
        public string User_GetFirstName(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("firstname", _context.Users.Where(a => a.ID == id).Single().First_Name));
            return message.ToString();
        }

        [HttpPut("{id:int}/firstname"), Authorize] // working
        public string User_EditFirstName(int id, [FromBody]string firstname)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

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

        [HttpGet("{id:int}/lastname"), Authorize] // working
        public string User_GetLastName(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("lastname", _context.Users.Where(a => a.ID == id).Single().Last_Name));
            return message.ToString();
        }

        [HttpPut("{id:int}/lastname"), Authorize] // working
        public string User_EditLastName(int id, [FromBody]string lastname)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

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

        [HttpDelete("{id:int}"), Authorize] // working
        public string User_DeleteUser(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

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

        // get all of the user's accounts
        [HttpGet("{id:int}/accounts"), Authorize] // working
        public string User_GetAccounts(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

            // format success response.. maybe could be done better but not sure yet
            JObject message = JObject.Parse(SuccessMessage._result);
            JArray accs = new JArray();
            foreach (Account acc in _context.Users.Single(a => a.ID == id).Accounts) { accs.Add(JToken.FromObject(new ReturnableAccount(acc))); }
            message.Add(new JProperty("accounts", accs));
            return message.ToString();
        }

        // add account.. input format is json
        [HttpPost("{id:int}/accounts"), Authorize] // in progress
        public string User_AddAccount(int id, [FromBody]string accJson) 
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

            return "";
        }
    }
}
