using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using Newtonsoft.Json.Linq;
using SafeAccountsAPI.Constants;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Models;

namespace SafeAccountsAPI.Controllers
{
    [ApiController]
    [Authorize]
    [Route("[controller]")]
    public class UsersController : Controller
    {
        private readonly APIContext _context; // database handle
        private readonly IHttpContextAccessor _httpContextAccessor; // handle to all http information.. used for authorization
        public IConfiguration _configuration; //

        // get an instance of a database and http handle
        public UsersController(APIContext context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration)
        {
            _context = context;
            _httpContextAccessor = httpContextAccessor;
            _configuration = configuration;
        }

        // login and get tokens...
        [HttpPost("login"), AllowAnonymous] //working
        public ActionResult User_Login([FromBody] Login login)
        {
            try
            {
                // get users saved password hash and salt
                User user = _context.Users.Single(a => a.Email == login.Email);

                // successful login.. compare user hash to the hash generated from the inputted password and salt
                if (ValidatePassword(login.Password, user.Password))
                {
                    string tokenString = HelperMethods.GenerateJWTAccessToken(user.Role, user.Email, _configuration.GetValue<string>("JwtTokenKey"));
                    RefreshToken refToken = HelperMethods.GenerateRefreshToken(user, _context);
                    LoginResponse rtrn = new LoginResponse { ID = user.ID, AccessToken = tokenString, RefreshToken = new ReturnableRefreshToken(refToken) };
                    _context.SaveChanges(); // always last on db to make sure nothing breaks and db has new info

                    // append cookies to response after login
                    HelperMethods.SetCookies(Response, tokenString, refToken);
                    return new OkObjectResult(rtrn);
                }
                else
                {
                    ErrorMessage error = new ErrorMessage("Invalid Credentials.", "Email or Password does not match.");
                    return new UnauthorizedObjectResult(error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error validating credentials", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // compare string input to store hash and salt combo
        private bool ValidatePassword(string input, byte[] storedPassword)
        {
            byte[] passwordHash = new byte[storedPassword.Length - HelperMethods.salt_length];
            byte[] salt = new byte[HelperMethods.salt_length]; ;
            Buffer.BlockCopy(storedPassword, 0, salt, 0, salt.Length); // get salt
            Buffer.BlockCopy(storedPassword, HelperMethods.salt_length, passwordHash, 0, passwordHash.Length); // get hash

            // successful login.. compare user hash to the hash generated from the inputted password and salt
            if (passwordHash.SequenceEqual(HelperMethods.GenerateSaltedHash(Encoding.UTF8.GetBytes(input), salt)))
                return true;
            else
                return false;
        }

        // logout and reset cookies.. I dont think here the ID of the user matters because we just delete all the associated cookies.
        [HttpPost("logout")] //working
        public IActionResult User_Logout()
        {
            try
            {
                // delete cookies
                foreach (var cookie in Request.Cookies)
                {
                    if (cookie.Key.Contains("SameSite"))
                    {
                        Response.Cookies.Delete(cookie.Key, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true,
                            SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None
                        });
                    }
                    else
                    {
                        Response.Cookies.Delete(cookie.Key, new CookieOptions
                        {
                            HttpOnly = true,
                            Secure = true
                        });
                    }
                }
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error removing users cookies.", ex.Message);
                return new InternalServerErrorResult(error);
            }

            return Ok();
        }

        // Get all available users.. might change later as it might not make sense to grab all accounts if there are tons
        [HttpGet] //working
        public IActionResult GetAllUsers()
        {
            if (!HelperMethods.ValidateIsAdmin(_httpContextAccessor))
            {
                ErrorMessage error = new ErrorMessage("Invalid Role", "Caller must have admin role.");
                return new UnauthorizedObjectResult(error);
            }

            // get and return all users
            List<ReturnableUser> users = new List<ReturnableUser>();
            try
            {
                foreach (User user in _context.Users.ToArray())
                {
                    ReturnableUser retUser = new ReturnableUser(user);
                    users.Add(retUser);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error retrieving users.", ex.Message);
                return new InternalServerErrorResult(error);
            }

            return new OkObjectResult(users);
        }

        // register new user
        [HttpPost, AllowAnonymous]
        public IActionResult User_AddUser([FromBody] NewUser newUser)
        {
            // if there is a user with this email already then we throw bad request error
            if (_context.Users.Single(a => a.Email == newUser.Email) != null)
            {
                ErrorMessage error = new ErrorMessage("Failed to create new user", "Email already in use.");
                return new BadRequestObjectResult(error);
            }

            // attempt to create new user and add to the database... later we need to implement hashing
            try
            {
                User userToRegister = new User(newUser); // new user with no accounts and registered as user
                _context.Users.Add(userToRegister);
                _context.SaveChanges();
                HelperMethods.CreateUserKeyandIV(_context.Users.Single(a => a.Email == newUser.Email).ID); // after we save changes, we need to get the user by their email and then use the id to create unique password and iv
                return Ok();
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to create new user", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // Get a specific user.
        [HttpGet("{id:int}")] // working
        public IActionResult User_GetUser(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            // strips out private data that is never to be sent back and returns user info
            ReturnableUser retUser = new ReturnableUser(_context.Users.Where(a => a.ID == id).Single());
            return new OkObjectResult(retUser);
        }

        [HttpDelete("{id:int}")] // working
        public IActionResult User_DeleteUser(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

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
                ErrorMessage error = new ErrorMessage("Failed to delete user.", ex.Message);
                return new InternalServerErrorResult(error);
            }

            return Ok();
        }

        [HttpGet("{id:int}/firstname")] // working
        public IActionResult User_GetFirstName(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            return new OkObjectResult(new { firstname = _context.Users.Where(a => a.ID == id).Single().First_Name });
        }

        [HttpPut("{id:int}/firstname")] // working
        public IActionResult User_EditFirstName(int id, [FromBody] string firstname)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            try
            {
                _context.Users.Where(a => a.ID == id).Single().First_Name = firstname;
                _context.SaveChanges();
                return new OkObjectResult(new { new_firstname = _context.Users.Where(a => a.ID == id).Single().First_Name });
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to update first name.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpGet("{id:int}/lastname")] // working
        public IActionResult User_GetLastName(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            return new OkObjectResult(new { lastname = _context.Users.Where(a => a.ID == id).Single().Last_Name });
        }

        [HttpPut("{id:int}/lastname")] // working
        public IActionResult User_EditLastName(int id, [FromBody] string lastname)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            try
            {
                _context.Users.Where(a => a.ID == id).Single().Last_Name = lastname;
                _context.SaveChanges();
                return new OkObjectResult(new { new_lastname = _context.Users.Where(a => a.ID == id).Single().Last_Name });
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to update last name.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpPut("{id:int}/password")]
        public IActionResult User_EditPassword(int id, [FromBody] PasswordReset psw_reset)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            try
            {
                // get password from db
                User user = _context.Users.Single(a => a.ID == id);

                // if password is valid then we change it and update db
                if (ValidatePassword(psw_reset.Current_Password, user.Password))
                {
                    user.Password = HelperMethods.ConcatenatedSaltAndSaltedHash(psw_reset.New_Password);
                    _context.Update(user);
                    _context.SaveChanges();
                    return Ok();
                }
                else
                {
                    ErrorMessage error = new ErrorMessage("Invalid Password", "Your current password does not match.");
                    return new BadRequestObjectResult(error);
                }
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to update with new password", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // get all of the user's accounts
        [HttpGet("{id:int}/accounts")] // working
        public IActionResult User_GetAccounts(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            // get and return all this user's accounts
            List<ReturnableAccount> accs = new List<ReturnableAccount>();
            try
            {
                foreach (Account acc in _context.Users.Single(a => a.ID == id).Accounts.ToArray())
                {
                    ReturnableAccount retAcc = new ReturnableAccount(acc);
                    accs.Add(retAcc);
                }
                return new OkObjectResult(accs);
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error retrieving accounts.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // add account.. input format is json
        [HttpPost("{id:int}/accounts")] // working
        public IActionResult User_AddAccount(int id, [FromBody] NewAccount accToAdd)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            try
            {
                // if this user does not own the folder we are adding to, then error
                if (accToAdd.FolderID != null && !_context.Users.Single(a => a.ID == id).Folders.Exists(b => b.ID == accToAdd.FolderID))
                {
                    ErrorMessage error = new ErrorMessage("Failed to create new account", "User does not have a folder matching that ID.");
                    return new BadRequestObjectResult(error);
                }

                // create new account and save it
                Account new_account = new Account(accToAdd, id);
                _context.Accounts.Add(new_account);
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error creating new account.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpDelete("{id:int}/accounts/{account_id:int}")] // working
        public IActionResult User_DeleteAccount(int id, int account_id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            try
            {
                // validate ownership of said account
                if (!_context.Users.Single(a => a.ID == id).Accounts.Exists(b => b.ID == account_id))
                {
                    ErrorMessage error = new ErrorMessage("Failed to delete account", "User does not have an account matching that ID.");
                    return new BadRequestObjectResult(error);
                }

                _context.Accounts.Remove(_context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id)); // fist match user id to ensure ownership
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error deleting account.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // get a specific accounts info
        [HttpGet("{id:int}/accounts/{account_id:int}")]
        public IActionResult User_GetSingleAccount(int id, int account_id)
        {
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                // validate ownership of said account
                if (!_context.Users.Single(a => a.ID == id).Accounts.Exists(b => b.ID == account_id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid account", "User does not have an account matching that ID.");
                    return new BadRequestObjectResult(error);
                }

                return new OkObjectResult(new ReturnableAccount(_context.Accounts.Single(a => a.ID == account_id)));
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error getting account", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // edit a specific accounts info
        [HttpPut("{id:int}/accounts/{account_id:int}/title")] // in progress
        public IActionResult User_EditAccountTitle(int id, int account_id, [FromBody] string title)
        {
            // attempt to edit the title
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                // validate ownership of said account
                if (!_context.Users.Single(a => a.ID == id).Accounts.Exists(b => b.ID == account_id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid account", "User does not have an account matching that ID.");
                    return new BadRequestObjectResult(error);
                }

                _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id).Title = title;
                _context.SaveChanges();
                return new OkObjectResult(new { new_title = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id).Title }); // return new title from db to confirm
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error editing title", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // edit a specific accounts info
        [HttpPut("{id:int}/accounts/{account_id:int}/login")]
        public IActionResult User_EditAccountLogin(int id, int account_id, [FromBody] string login)
        {
            // attempt to edit the login
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                // validate ownership of said account
                if (!_context.Users.Single(a => a.ID == id).Accounts.Exists(b => b.ID == account_id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid account", "User does not have an account matching that ID.");
                    return new BadRequestObjectResult(error);
                }

                _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id).Login = login;
                _context.SaveChanges();
                return new OkObjectResult(new { new_login = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id).Login });
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error editing login", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // edit a specific accounts info
        [HttpPut("{id:int}/accounts/{account_id:int}/password")] // in progress
        public string User_EditAccountPassword(int id, int account_id, [FromBody] string password)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                Response.StatusCode = 401;
                return JObject.FromObject(new ErrorMessage("Invalid User", "Caller can only access their information.")).ToString();
            }

            try
            {
                Account acc = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
                acc.Password = HelperMethods.EncryptStringToBytes_Aes(password, HelperMethods.GetUserKeyAndIV(id)); // this logic will need to be changed to use a unique key
                _context.Accounts.Update(acc);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return JObject.FromObject(new ErrorMessage("Error editing password", ex.Message)).ToString();
            }

            return SuccessMessage.Result;
        }

        // edit a specific accounts info
        [HttpPut("{id:int}/accounts/{account_id:int}/description")]
        public string User_EditAccountDesc(int id, int account_id, [FromBody] string description)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                Response.StatusCode = 401;
                return JObject.FromObject(new ErrorMessage("Invalid User", "Caller can only access their information.")).ToString();
            }

            // attempt to edit the description
            try
            {
                Account acc = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
                acc.Description = description;
                _context.Accounts.Update(acc);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return JObject.FromObject(new ErrorMessage("Error editing description", ex.Message)).ToString();
            }

            return SuccessMessage.Result;
        }

        [HttpPut("{id:int}/accounts/{account_id:int}/folder")]
        public string User_AccountSetFolder(int id, int account_id, [FromBody] string folder_id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                Response.StatusCode = 401;
                return JObject.FromObject(new ErrorMessage("Invalid User", "Caller can only access their information.")).ToString();
            }

            // attempt to edit the description
            try
            {
                Account acc = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);

                // left empty implies removing any associated folder
                if (string.IsNullOrWhiteSpace(folder_id))
                    acc.FolderID = null;
                else
                { // here we have to validate that the user owns the folder
                    acc.FolderID = _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == int.Parse(folder_id)).ID; // we code it like this to make sure that whatever folder we attempt exists and is owner by this user
                }

                _context.Accounts.Update(acc);
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return JObject.FromObject(new ErrorMessage("Error settting folder", ex.Message)).ToString();
            }

            return SuccessMessage.Result;
        }

        [HttpGet("{id:int}/folders")]
        public string User_GetFolders(int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                Response.StatusCode = 401;
                return JObject.FromObject(new ErrorMessage("Invalid User", "Caller can only access their information.")).ToString();
            }

            // format success response.. maybe could be done better but not sure yet
            JObject message = JObject.Parse(SuccessMessage.Result);
            JArray folders = new JArray();
            foreach (Folder fold in _context.Users.Single(a => a.ID == id).Folders) { folders.Add(JToken.FromObject(new ReturnableFolder(fold))); }
            message.Add(new JProperty("folders", folders));
            return message.ToString();
        }

        [HttpPost("{id:int}/folders")] // working
        public string User_AddFolder(int id, [FromBody] string folderJson)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                Response.StatusCode = 401;
                return JObject.FromObject(new ErrorMessage("Invalid User", "Caller can only access their information.")).ToString();
            }

            JObject json = null;

            // might want Json verification as own function since all will do it.. we will see
            try { json = JObject.Parse(folderJson); }
            catch (Exception ex)
            {
                Response.StatusCode = 400;
                ErrorMessage error = new ErrorMessage("Invalid Json", ex.Message);
                return JObject.FromObject(error).ToString();
            }

            try
            {
                int? pid = json["parent_id"]?.ToObject<int?>(); // parent id

                // if user doesnt own the parent or isnt currently admin, we throw error
                if (pid != null && _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == pid) == null && !HelperMethods.ValidateIsAdmin(_httpContextAccessor))
                    throw new Exception("User must own the parent folder or be admin");

                // use token in header to to 
                Folder new_folder = new Folder { UserID = id, FolderName = json["folder_name"].ToString(), ParentID = pid };
                _context.Folders.Add(new_folder); // add new folder

                // only update parent if needed
                if (pid != null)
                {
                    Folder parent_folder = _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == pid); // this makes sure that the parent folder is owned by our user
                    parent_folder.HasChild = true;
                    _context.Folders.Update(parent_folder); // register to parent that is now has at least 1 child
                }
                _context.SaveChanges();
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return JObject.FromObject(new ErrorMessage("Error creating new folder.", ex.Message)).ToString();
            }

            return SuccessMessage.Result;
        }

        // delete a folder and all contents if it is not empty
        [HttpDelete("{id:int}/folders/{folder_id:int}")] // working
        public string User_DeleteFolder(int id, int folder_id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                Response.StatusCode = 401;
                return JObject.FromObject(new ErrorMessage("Invalid User", "Caller can only access their information.")).ToString();
            }

            try
            {
                Folder folderToDelete = _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == folder_id);
                // if this folder has children, then we need to call DeleteFolder on all children
                if (folderToDelete.HasChild)
                {
                    List<Folder> folders = _context.Users.Single(a => a.ID == id).Folders.ToList<Folder>();
                    foreach (Folder folder in folders)
                    {
                        if (folder.ParentID == folderToDelete.ID)
                        {
                            User_DeleteFolder(id, folder.ID); // recursive call to go down the tree and delete children
                        }
                    }
                }

                // delete the accounts in the folder
                List<Account> accounts = _context.Users.Single(a => a.ID == id).Accounts.ToList<Account>();
                foreach (Account account in accounts)
                {
                    if (account.FolderID == folderToDelete.ID)
                    {
                        _context.Accounts.Remove(account); // no need to call User_DeleteAccount because identity and access token have already been verifies
                    }
                }
                _context.SaveChanges(); // save the accounts being deleted
                _context.Folders.Remove(folderToDelete); // remove the folder
                _context.SaveChanges(); // save the folder being deleted.. must be done seperate because of foreign keys
            }
            catch (Exception ex)
            {
                Response.StatusCode = 500;
                return JObject.FromObject(new ErrorMessage("Error deleting folder.", ex.Message)).ToString();
            }

            return SuccessMessage.Result;
        }
    }
}
