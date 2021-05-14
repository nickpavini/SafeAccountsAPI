﻿using System;
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
            try
            {
                if (!HelperMethods.ValidateIsAdmin(_httpContextAccessor))
                {
                    ErrorMessage error = new ErrorMessage("Invalid Role", "Caller must have admin role.");
                    return new UnauthorizedObjectResult(error);
                }

                // get and return all users
                List<ReturnableUser> users = new List<ReturnableUser>();
                foreach (User user in _context.Users.ToArray())
                {
                    ReturnableUser retUser = new ReturnableUser(user);
                    users.Add(retUser);
                }

                return new OkObjectResult(users);
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error retrieving users.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // register new user
        [HttpPost, AllowAnonymous]
        public IActionResult User_AddUser([FromBody] NewUser newUser)
        {
            // attempt to create new user and add to the database... later we need to implement hashing
            try
            {
                // if there is a user with this email already then we throw bad request error
                if (_context.Users.Single(a => a.Email == newUser.Email) != null)
                {
                    ErrorMessage error = new ErrorMessage("Failed to create new user", "Email already in use.");
                    return new BadRequestObjectResult(error);
                }

                User userToRegister = new User(newUser); // new user with no accounts and registered as user
                _context.Users.Add(userToRegister);
                _context.SaveChanges();
                HelperMethods.CreateUserKeyandIV(_context.Users.Single(a => a.Email == newUser.Email).ID); // after we save changes, we need to get the user by their email and then use the id to create unique password and iv
                return Ok();
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error creating new user", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // Get a specific user.
        [HttpGet("{id:int}")] // working
        public IActionResult User_GetUser(int id)
        {
            try
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
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to get user.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpDelete("{id:int}")] // working
        public IActionResult User_DeleteUser(int id)
        {
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                // attempt to remove all data and update changes
                _context.Accounts.RemoveRange(_context.Accounts.Where(a => a.UserID == id));
                _context.RefreshTokens.RemoveRange(_context.RefreshTokens.Where(a => a.UserID == id));
                _context.Users.Remove(_context.Users.Single(a => a.ID == id));
                _context.SaveChanges();

                return Ok();
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to delete user.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpGet("{id:int}/firstname")] // working
        public IActionResult User_GetFirstName(int id)
        {
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                return new OkObjectResult(new { firstname = _context.Users.Where(a => a.ID == id).Single().First_Name });
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to get first name.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpPut("{id:int}/firstname")] // working
        public IActionResult User_EditFirstName(int id, [FromBody] string firstname)
        {
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

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
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                return new OkObjectResult(new { lastname = _context.Users.Where(a => a.ID == id).Single().Last_Name });
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Failed to get last name.", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpPut("{id:int}/lastname")] // working
        public IActionResult User_EditLastName(int id, [FromBody] string lastname)
        {
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

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
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                // get user from db
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
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                // get and return all this user's accounts
                List<ReturnableAccount> accs = new List<ReturnableAccount>();
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
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

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
        public IActionResult User_EditAccountPassword(int id, int account_id, [FromBody] string password)
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

                _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id).Password = HelperMethods.EncryptStringToBytes_Aes(password, HelperMethods.GetUserKeyAndIV(id));
                _context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error editing password", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        // edit a specific accounts info
        [HttpPut("{id:int}/accounts/{account_id:int}/description")]
        public IActionResult User_EditAccountDesc(int id, int account_id, [FromBody] string description)
        {
            // attempt to edit the description
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

                _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id).Description = description;
                _context.SaveChanges();
                return new OkObjectResult(new { new_description = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id).Description });
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error editing description", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpPut("{id:int}/accounts/{account_id:int}/folder")]
        public IActionResult User_AccountSetFolder(int id, int account_id, [FromBody] int? folder_id)
        {
            // attempt to edit the description
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

                // use zero to mean null since body paramter must be present
                if (folder_id == 0)
                    folder_id = null;

                // if this user does not own the folder we are adding to, then error
                if (folder_id != null && !_context.Users.Single(a => a.ID == id).Folders.Exists(b => b.ID == folder_id))
                {
                    ErrorMessage error = new ErrorMessage("Failed to create new account", "User does not have a folder matching that ID.");
                    return new BadRequestObjectResult(error);
                }
                else
                {
                    _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id).FolderID = folder_id;
                    _context.SaveChanges();
                }

                return new OkObjectResult(new { new_folder = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id).FolderID });
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error settting folder", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpGet("{id:int}/folders")]
        public IActionResult User_GetFolders(int id)
        {
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                // get and return all this user's accounts
                List<ReturnableFolder> folders = new List<ReturnableFolder>();
                foreach (Folder fold in _context.Users.Single(a => a.ID == id).Folders.ToArray())
                {
                    ReturnableFolder retFold = new ReturnableFolder(fold);
                    folders.Add(retFold);
                }

                return new OkObjectResult(folders);
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error getting folders", ex.Message);
                return new InternalServerErrorResult(error);
            }
        }

        [HttpPost("{id:int}/folders")] // working
        public IActionResult User_AddFolder(int id, [FromBody] NewFolder folderToAdd)
        {
            try
            {
                // verify that the user is either admin or is requesting their own data
                if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
                {
                    ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                    return new UnauthorizedObjectResult(error);
                }

                folderToAdd.Parent_ID = folderToAdd.Parent_ID == 0 ? null : folderToAdd.Parent_ID; // parent id goes from 0 to null for simplicity

                // if user doesnt own the parent we throw error
                if (folderToAdd.Parent_ID != null && !_context.Users.Single(a => a.ID == id).Folders.Exists(b => b.ID == folderToAdd.Parent_ID))
                {
                    ErrorMessage error = new ErrorMessage("Invalid parent ID", "User does not have a folder with that ID");
                    return new BadRequestObjectResult(error);
                }

                // use token in header to to 
                Folder new_folder = new Folder(folderToAdd, id);
                _context.Folders.Add(new_folder); // add new folder

                // only update parent if needed
                if (new_folder.ParentID != null)
                    _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == new_folder.ParentID).HasChild = true;// set parent to having child

                _context.SaveChanges();
                return Ok();
            }
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Error creating new folder.", ex.Message);
                return new InternalServerErrorResult(error);
            }
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
