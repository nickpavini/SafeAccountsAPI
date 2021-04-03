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
using System.Text;
using System.Security.Cryptography;
using Microsoft.Net.Http.Headers;

// For more information on enabling Web API for empty projects, visit https://go.microsoft.com/fwlink/?LinkID=397860

namespace SafeAccountsAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class UsersController : Controller
    {
        private readonly APIContext _context; // database handle
        private readonly IHttpContextAccessor _httpContextAccessor; // handle to all http information.. used for authorization

        // get an instance of a database and http handle
        public UsersController(APIContext context, IHttpContextAccessor httpContextAccessor) { 
            _context = context;
            _httpContextAccessor = httpContextAccessor;
        }

        // login and get tokens...
        [HttpPost("login"), AllowAnonymous] //working
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
                // get users saved password hash and salt
                User user = _context.Users.Single(a => a.Email == json["email"].ToString());

                // successful login.. compare user hash to the hash generated from the inputted password and salt
                if (ValidatePassword(json["password"].ToString(), user.Password))
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

        private bool ValidatePassword(string input, byte[] storedPassword)
        {
            byte[] passwordHash = new byte[storedPassword.Length - HelperMethods.salt_length];
            byte[] salt = new byte[HelperMethods.salt_length]; ;
            Buffer.BlockCopy(storedPassword, 0, salt, 0, salt.Length);
            Buffer.BlockCopy(storedPassword, HelperMethods.salt_length, passwordHash, 0, passwordHash.Length);

            // successful login.. compare user hash to the hash generated from the inputted password and salt
            if (passwordHash.SequenceEqual(HelperMethods.GenerateSaltedHash(Encoding.UTF8.GetBytes(input), salt)))
                return true;
            else
                return false;
        }

        // Get all available users.. might change later as it might not make sense to grab all accounts if there are tons
        [HttpGet] //working
        public string GetAllUsers()
        {
            if (!HelperMethods.ValidateIsAdmin(_httpContextAccessor))
                return JObject.FromObject(new ErrorMessage("Invalid Role", "n/a", "Caller must have admin role.")).ToString(); // n/a for no args there

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

        // register new user
        [HttpPost, AllowAnonymous] // Working.. needs password hashing
        public string User_AddUser([FromBody]string userJson)
        {
            JObject json = null;

            // might want Json verification as own function since all will do it.. we will see
            try { json = JObject.Parse(userJson); }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Invalid Json", userJson, ex.Message);
                return JObject.FromObject(error).ToString();
            }

            // attempt to create new user and add to the database... later we need to implement hashing
            try
            {
                // hash password with salt.. still trying to understand a bit about the difference between unicode and base 64 string so for now we are just dealing with byte arrays
                byte[] salt = HelperMethods.CreateSalt(HelperMethods.salt_length);
                byte[] password = HelperMethods.GenerateSaltedHash(Encoding.UTF8.GetBytes(json["password"].ToString()), salt);
                byte[] concatenated = new byte[salt.Length + password.Length];
                Buffer.BlockCopy(salt, 0, concatenated, 0, salt.Length);
                Buffer.BlockCopy(password, 0, concatenated, salt.Length, password.Length);

                User newUser = new User { First_Name = json["firstname"].ToString(), Last_Name = json["lastname"].ToString(), Email = json["email"].ToString(), Password = concatenated, NumAccs = 0, Role = UserRoles.User };
                _context.Users.Add(newUser);
                _context.SaveChanges();
            }
            catch(Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to create new user", json.ToString(), ex.Message);
                return JObject.FromObject(error).ToString();
            }

            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("id", _context.Users.Single(a => a.Email == json["email"].ToString()).ID)); // user context to get id since locally created user will not have id set
            return message.ToString();
        }

        // Get a specific user.
        [HttpGet("{id:int}")] // working
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

        [HttpDelete("{id:int}")] // working
        public string User_DeleteUser(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

            try
            {
                // attempt to remove all data and update changes
                _context.Accounts.RemoveRange(_context.Accounts.Where(a => a.UserID == id));
                _context.RefreshTokens.RemoveRange(_context.RefreshTokens.Where(a => a.UserID == id));
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

        [HttpGet("{id:int}/firstname")] // working
        public string User_GetFirstName(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("firstname", _context.Users.Where(a => a.ID == id).Single().First_Name));
            return message.ToString();
        }

        [HttpPut("{id:int}/firstname")] // working
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

        [HttpGet("{id:int}/lastname")] // working
        public string User_GetLastName(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("lastname", _context.Users.Where(a => a.ID == id).Single().Last_Name));
            return message.ToString();
        }

        [HttpPut("{id:int}/lastname")] // working
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

        [HttpPut("{id:int}/password")]
        public string User_EditPassword(int id, [FromBody]string passwordJson)
        {
            JObject json = null;

            // might want Json verification as own function since all will do it.. we will see
            try { json = JObject.Parse(passwordJson); }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Invalid Json", passwordJson, ex.Message);
                return JObject.FromObject(error).ToString();
            }

            try
            {
                User user = HelperMethods.GetUserFromAccessToken(Request.Headers[HeaderNames.Authorization].ToString().Replace("Bearer ", ""), _context);

                // if password is valid then we change it
                if (ValidatePassword(json["current_password"].ToString(), user.Password))
                {
                    // get salt
                    byte[] salt = new byte[HelperMethods.salt_length];
                    Buffer.BlockCopy(user.Password, 0, salt, 0, salt.Length);

                    // generate new hash and concatenate
                    byte[] newPassHash = HelperMethods.GenerateSaltedHash(Encoding.UTF8.GetBytes(json["new_password"].ToString()), salt);
                    byte[] concatenated = new byte[salt.Length + newPassHash.Length];
                    Buffer.BlockCopy(salt, 0, concatenated, 0, salt.Length);
                    Buffer.BlockCopy(newPassHash, 0, concatenated, salt.Length, newPassHash.Length);

                    //assign and update db
                    user.Password = concatenated;
                    _context.Update(user);
                    _context.SaveChanges();
                }
                else
                    return JObject.FromObject(new ErrorMessage("Invalid Password", json["current_password"].ToString(), "n/a")).ToString();
            }
            catch(Exception ex)
            {
                return JObject.FromObject(new ErrorMessage("Failed to update with new password", "n/a", ex.Message)).ToString(); // don't continue to send password back and forth in messages
            }


            return JObject.Parse(SuccessMessage._result).ToString();
        }

        // get all of the user's accounts
        [HttpGet("{id:int}/accounts")] // working
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
        [HttpPost("{id:int}/accounts")] // in progress
        public string User_AddAccount(int id, [FromBody]string accJson) 
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();

            return "";
        }
    }
}
