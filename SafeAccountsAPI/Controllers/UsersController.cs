using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Microsoft.Extensions.Configuration;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Helpers;
using SafeAccountsAPI.Models;
using SafeAccountsAPI.Filters;

namespace SafeAccountsAPI.Controllers
{
    [ApiController]
    [Authorize(Policy = "LoggedIn")]
    [Authorize(Policy = "ApiJwtToken")]
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

        // register new user
        [HttpPost, AllowAnonymous]
        [ApiExceptionFilter("Error creating new user")]
        public IActionResult User_AddUser([FromBody] NewUser newUser)
        {
            // attempt to create new user and add to the database.

            // if there is a user with this email already then we throw bad request error
            if (_context.Users.SingleOrDefault(a => a.Email == newUser.Email) != null)
            {
                ErrorMessage error = new ErrorMessage("Failed to create new user", "Email already in use.");
                return new BadRequestObjectResult(error);
            }

            User userToRegister = new User(newUser); // new user with no accounts and registered as user
            _context.Users.Add(userToRegister);
            _context.SaveChanges();

            // after we save changes, we need to create unique key and iv, then send the confirmation email
            HelperMethods.CreateUserKeyandIV(_context.Users.Single(a => a.Email == newUser.Email).ID);
            SendConfirmationEmail(userToRegister);
            return Ok();
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="user"></param>
        /// <remarks>TODO Move this to the helpers</remarks>
        private void SendConfirmationEmail(User user)
        {
            // generate token
            string token = HelperMethods.GenerateJWTEmailConfirmationToken(user.Email, _configuration.GetValue<string>("EmailConfirmationTokenKey"));

            // handle to our smtp client
            var smtpClient = new SmtpClient(_configuration.GetValue<string>("Smtp:Host"))
            {
                Port = int.Parse(_configuration.GetValue<string>("Smtp:Port")),
                Credentials = new NetworkCredential(_configuration.GetValue<string>("Smtp:Username"), _configuration.GetValue<string>("Smtp:Password")),
                EnableSsl = true,
            };

            // format the body of the message
            string body = "Hello " + user.First_Name + ",\n\n";
            body += "A new account has been registered with SafeAccounts using your email address.\n\n";
            body += "To confirm your new account, please go to this web address:\n\n";
            body += _configuration.GetValue<string>("WebsiteUrl") + "emailconfirmation/?token=" + token + "&email=" + user.Email;
            body += "\n\nThis should appear as a blue link which you can just click on. If that doesn't work,";
            body += "then cut and paste the address into the address line at the top of your web browser window.\n\n";
            body += "If you need help, please contact the site administrator.\n\n";
            body += "SafeAccounts Administrator,\n";
            body += _configuration.GetValue<string>("Smtp:Username");

            // handle to our message settings
            var mailMessage = new MailMessage
            {
                From = new MailAddress(_configuration.GetValue<string>("Smtp:Username")),
                Subject = "Confirm Your SafeAccounts Registration",
                Body = body,
                IsBodyHtml = false,
            };
            mailMessage.To.Add(user.Email);

            // send message
            smtpClient.Send(mailMessage);
        }

        [HttpPost("confirm"), AllowAnonymous] //working
        [ApiExceptionFilter("Error confirming email")]
        public ActionResult User_ConfirmEmail(string token, string email)
        {

            User userToConfirm = _context.Users.Single(a => a.Email == email);

            // check if email is already verified
            if (userToConfirm.EmailVerified)
            {
                ErrorMessage error = new ErrorMessage("Failed to confirm email", "Email address is already confirmed.");
                return new BadRequestObjectResult(error);
            }

            // verify that the email provided matches the email in the token.
            if (email != HelperMethods.GetUserFromAccessToken(token, _context, _configuration.GetValue<string>("EmailConfirmationTokenKey")).Email)
            {
                ErrorMessage error = new ErrorMessage("Failed to confirm email", "Token is invalid.");
                return new BadRequestObjectResult(error);
            }

            // ok now we save the users email as verified
            userToConfirm.EmailVerified = true;
            _context.Users.Update(userToConfirm);
            _context.SaveChanges();
            return Ok();

        }

        // login and get tokens...
        [HttpPost("login"), AllowAnonymous] //working
        [ApiExceptionFilter("Error validating credentials")]
        public ActionResult User_Login([FromBody] Login login)
        {
            // get users saved password hash and salt
            User user = _context.Users.Single(a => a.Email == login.Email);

            // check if the user has a verified email or not
            if (!user.EmailVerified)
            {
                ErrorMessage error = new ErrorMessage("Email unconfirmed.", "Please confirm email first.");
                return new UnauthorizedObjectResult(error);
            }

            // successful login.. compare user hash to the hash generated from the inputted password and salt
            if (ValidatePassword(login.Password, user.Password))
            {
                string tokenString = HelperMethods.GenerateJWTAccessToken(user.Role, user.Email, _configuration.GetValue<string>("UserJwtTokenKey"));
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


        /// <summary>
        /// Compare string input to store hash and salt combo 
        /// </summary>
        /// <param name="input"></param>
        /// <param name="storedPassword"></param>
        /// <returns></returns>
        /// <remarks> TODO move this to Helper</remarks>
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
        [ApiExceptionFilter("Error removing users cookies.")]
        public IActionResult User_Logout()
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
            return Ok();
        }


        // <summary>
        /// Get all available users.. might change later as it might not make sense to grab all accounts if there are tons
        /// </summary>
        /// <returns></returns>
        [HttpGet] //working
        [ApiExceptionFilter("Error retrieving users.")]
        public IActionResult GetAllUsers()
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

        // Get a specific user.
        [HttpGet("{id:int}")] // working
        [ApiExceptionFilter("Failed to get user.")]
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


        [HttpDelete("{id:int}")]// working
        [ApiExceptionFilter("Failed to delete user.")]
        public IActionResult User_DeleteUser(int id)
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

        [HttpGet("{id:int}/firstname")] // working
        [ApiExceptionFilter("Failed to get first name.")]
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
        [ApiExceptionFilter("Failed to update first name.")]
        public IActionResult User_EditFirstName(int id, [FromBody] string firstname)
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

        [HttpGet("{id:int}/lastname")] // working
        [ApiExceptionFilter("Failed to get last name.")]
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
        [ApiExceptionFilter("Failed to update last name.")]
        public IActionResult User_EditLastName(int id, [FromBody] string lastname)
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

        [HttpPut("{id:int}/password")]
        [ApiExceptionFilter("Failed to update with new password")]
        public IActionResult User_EditPassword(int id, [FromBody] PasswordReset psw_reset)
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

        // get all of the user's accounts
        [HttpGet("{id:int}/accounts")] // working
        [ApiExceptionFilter("Error retrieving accounts.")]
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
            foreach (Account acc in _context.Users.Single(a => a.ID == id).Accounts.ToArray())
            {
                ReturnableAccount retAcc = new ReturnableAccount(acc);
                accs.Add(retAcc);
            }
            return new OkObjectResult(accs);

        }

        // add account.. input format is json
        [HttpPost("{id:int}/accounts")] // working
        [ApiExceptionFilter("Error creating new account.")]
        public IActionResult User_AddAccount(int id, [FromBody] NewAccount accToAdd)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            // account limit is 50 for now
            if (_context.Users.Single(a => a.ID == id).Accounts.Count >= 50)
            {
                ErrorMessage error = new ErrorMessage("Failed to create new account", "User cannot have more than 50 passwords saved at once.");
                return new BadRequestObjectResult(error);
            }

            // if this user does not own the folder we are adding to, then error
            if (accToAdd.FolderID != null && !_context.Users.Single(a => a.ID == id).Folders.Exists(b => b.ID == accToAdd.FolderID))
            {
                ErrorMessage error = new ErrorMessage("Failed to create new account", "User does not have a folder matching that ID.");
                return new BadRequestObjectResult(error);
            }

            // create new account and save it
            Account new_account = new Account(accToAdd, id);
            new_account.LastModified = DateTime.Now.ToString();
            _context.Accounts.Add(new_account);
            _context.SaveChanges();
            return Ok();
        }

        // this is different than calling delete account over and over. Here we only save once
        [HttpDelete("{id:int}/accounts")] // working
        [ApiExceptionFilter("Error deleting accounts.")]
        public IActionResult User_DeleteMultipleAccounts(int id, [FromBody] List<int> account_ids)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            foreach (int acc_id in account_ids)
            {
                // validate ownership of said account
                if (!_context.Users.Single(a => a.ID == id).Accounts.Exists(b => b.ID == acc_id))
                {
                    ErrorMessage error = new ErrorMessage("Failed to delete accounts", "User does not have an account matching ID: " + acc_id);
                    return new BadRequestObjectResult(error);
                }

                _context.Accounts.Remove(_context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == acc_id)); // fist match user id to ensure ownership
            }
            _context.SaveChanges();
            return Ok();
        }

        [HttpDelete("{id:int}/accounts/{account_id:int}")] // working
        [ApiExceptionFilter("Error deleting account.")]
        public IActionResult User_DeleteAccount(int id, int account_id)
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

        // get a specific accounts info
        [HttpGet("{id:int}/accounts/{account_id:int}")]
        [ApiExceptionFilter("Error getting account.")]
        public IActionResult User_GetSingleAccount(int id, int account_id)
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

        // set whether the specific account is a favorite or not
        [HttpPut("{id:int}/accounts/{account_id:int}/favorite")]
        [ApiExceptionFilter("Error favoriting the account.")]
        public IActionResult User_EditAccountIsFavorite(int id, int account_id, [FromBody] bool isFavorite)
        {
            // attempt to set account to be favorite or not
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

            // get account and set favorite setting.. here we wont see it as the account has been modified
            Account accToEdit = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
            accToEdit.IsFavorite = isFavorite;
            _context.SaveChanges();

            return Ok();
        }

        // edit a specific accounts info
        [HttpPut("{id:int}/accounts/{account_id:int}/title")] // in progress
        [ApiExceptionFilter("Error editing title")]
        public IActionResult User_EditAccountTitle(int id, int account_id, [FromBody] string title)
        {
            // attempt to edit the title

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

            // get account and modify
            Account accToEdit = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
            accToEdit.Title = title;
            accToEdit.LastModified = DateTime.Now.ToString();
            _context.SaveChanges();
            return Ok();
        }

        // edit a specific accounts info
        [HttpPut("{id:int}/accounts/{account_id:int}/login")]
        [ApiExceptionFilter("Error editing login")]
        public IActionResult User_EditAccountLogin(int id, int account_id, [FromBody] string login)
        {
            // attempt to edit the login

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

            // get account and modify
            Account accToEdit = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
            accToEdit.Login = login;
            accToEdit.LastModified = DateTime.Now.ToString();
            _context.SaveChanges();
            return Ok();
        }

        // edit a specific accounts info
        [HttpPut("{id:int}/accounts/{account_id:int}/password")] // in progress
        [ApiExceptionFilter("Error editing password")]
        public IActionResult User_EditAccountPassword(int id, int account_id, [FromBody] string password)
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


            // get account and modify
            Account accToEdit = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
            accToEdit.Password = HelperMethods.EncryptStringToBytes_Aes(password, HelperMethods.GetUserKeyAndIV(id));
            accToEdit.LastModified = DateTime.Now.ToString();
            _context.SaveChanges();
            return Ok();
        }

        // edit a specific accounts info
        [HttpPut("{id:int}/accounts/{account_id:int}/description")]
        [ApiExceptionFilter("Error editing description")]
        public IActionResult User_EditAccountDesc(int id, int account_id, [FromBody] string description)
        {
            // attempt to edit the description
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

            // get account and modify
            Account accToEdit = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
            accToEdit.Description = description;
            accToEdit.LastModified = DateTime.Now.ToString();
            _context.SaveChanges();

            return Ok();
        }

        [HttpPut("{id:int}/accounts/{account_id:int}/folder")]
        [ApiExceptionFilter("Error settting folder")]
        public IActionResult User_AccountSetFolder(int id, int account_id, [FromBody] int? folder_id)
        {
            // attempt to edit the description
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

        [HttpGet("{id:int}/folders")]
        [ApiExceptionFilter("Error getting folders")]
        public IActionResult User_GetFolders(int id)
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

        [HttpPost("{id:int}/folders")] // working
        [ApiExceptionFilter("Error creating new folder.")]
        public IActionResult User_AddFolder(int id, [FromBody] NewFolder folderToAdd)
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

        // delete a folder and all contents if it is not empty
        [HttpDelete("{id:int}/folders/{folder_id:int}")] // working
        [ApiExceptionFilter("Error deleting folder")]
        public IActionResult User_DeleteFolder(int id, int folder_id)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            // validate ownership of said folder
            if (!_context.Users.Single(a => a.ID == id).Folders.Exists(b => b.ID == folder_id))
            {
                ErrorMessage error = new ErrorMessage("Invalid folder", "User does not have a folder matching that ID.");
                return new BadRequestObjectResult(error);
            }

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

            // if parent isnt root, then check if parent still has children
            if (folderToDelete.ParentID != null)
            {
                bool parent_has_children = false;
                List<Folder> folders = _context.Users.Single(a => a.ID == id).Folders.ToList<Folder>();
                foreach (Folder fold in folders)
                {
                    // if this folders parent is the same as the one we were just deleting than the original parent still has kids
                    if (fold.ParentID == folderToDelete.ParentID)
                        parent_has_children = true;
                }

                // update parent if needed
                if (!parent_has_children)
                {
                    _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == folderToDelete.ParentID).HasChild = false;
                    _context.SaveChanges();
                }
            }

            // get and return all this user's folders...
            // this is helpful to have here because deleting one folder could affect others and we dont
            // want to redo the parsing logic UI side or make more api calls
            List<ReturnableFolder> updatedFolders = new List<ReturnableFolder>();
            foreach (Folder fold in _context.Users.Single(a => a.ID == id).Folders.ToArray())
            {
                ReturnableFolder retFold = new ReturnableFolder(fold);
                updatedFolders.Add(retFold);
            }

            // get and return all this user's accounts
            List<ReturnableAccount> safe = new List<ReturnableAccount>();
            foreach (Account acc in _context.Users.Single(a => a.ID == id).Accounts.ToArray())
            {
                ReturnableAccount retAcc = new ReturnableAccount(acc);
                safe.Add(retAcc);
            }

            // return both as an anonomous type
            var test = new { safe, updatedFolders };
            return new OkObjectResult(test);
        }

        // edit a specific folders name
        [HttpPut("{id:int}/folders/{folder_id:int}/name")] // in progress
        [ApiExceptionFilter("Error editing folder name")]
        public IActionResult User_EditFolderName(int id, int folder_id, [FromBody] string name)
        {
            // attempt to edit the title

            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            // validate ownership of said folder
            if (!_context.Users.Single(a => a.ID == id).Folders.Exists(b => b.ID == folder_id))
            {
                ErrorMessage error = new ErrorMessage("Invalid Folder", "User does not have a folder matching that ID.");
                return new BadRequestObjectResult(error);
            }

            // modify
            _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == folder_id).FolderName = name;
            _context.SaveChanges();
            return Ok();
        }

        // set a folders parent
        [HttpPut("{id:int}/folders/{folder_id:int}/parent")]
        [ApiExceptionFilter("Error editing the folders parent.")]
        public IActionResult User_SetFolderParent(int id, int folder_id, [FromBody] int? newParentID)
        {
            // verify that the user is either admin or is requesting their own data
            if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id))
            {
                ErrorMessage error = new ErrorMessage("Invalid User", "Caller can only access their information.");
                return new UnauthorizedObjectResult(error);
            }

            // validate ownership of child
            if (!_context.Users.Single(a => a.ID == id).Folders.Exists(b => b.ID == folder_id))
            {
                ErrorMessage error = new ErrorMessage("Invalid folder", "User does not have a folder matching that ID.");
                return new BadRequestObjectResult(error);
            }

            // use zero to mean null since body paramter must be present
            if (newParentID == 0)
                newParentID = null;

            // validate ownership of parent
            if (newParentID != null && !_context.Users.Single(a => a.ID == id).Folders.Exists(b => b.ID == newParentID))
            {
                ErrorMessage error = new ErrorMessage("Invalid folder", "User does not have a folder matching the ID of the new parent.");
                return new BadRequestObjectResult(error);
            }

            /*
             * We need to be careful here that there are no issues when we move up the tree.
             * The new parent cannot be a child of the folder we wish to give a new parent.
             */
            if (CheckFolderForParentClash(id, folder_id, newParentID))
            {
                ErrorMessage error = new ErrorMessage("Invalid new parent folder", "Folder cannot be a child of itself or one of its children.");
                return new BadRequestObjectResult(error);
            }

            // get reference to child old parent
            Folder child = _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == folder_id);
            Folder oldParent = child.ParentID != null ? _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == child.ParentID) : null;

            // set the childs new parentID, and set new parent to have a child
            child.ParentID = newParentID;
            if (newParentID != null)
                _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == newParentID).HasChild = true;

            // Here we will want to check the old parent to make sure it still has children
            if (oldParent != null)
            {
                // if no other folder exists with their parent id the same as the old parent id, we update it to not have children
                if (!_context.Users.Single(a => a.ID == id).Folders.Exists(b => b.ParentID == oldParent.ID && b.ID != child.ID))
                    oldParent.HasChild = false;
            }

            _context.SaveChanges();

            // get and return all this user's folders...
            // this is helpful to have here because changin one folder could affect others and we dont
            // want to redo the parsing logic UI side
            List<ReturnableFolder> folders = new List<ReturnableFolder>();
            foreach (Folder fold in _context.Users.Single(a => a.ID == id).Folders.ToArray())
            {
                ReturnableFolder retFold = new ReturnableFolder(fold);
                folders.Add(retFold);
            }
            return new OkObjectResult(folders);
        }

        // returns true if their is a clash of parents, and false if no clashes
        private bool CheckFolderForParentClash(int uid, int folder_id, int? parentID)
        {
            // if we want to set the parent as null, then no clash
            if (parentID == null)
                return false;

            // if the parent id is the folder, clash because a folder cannot be a child of itself or one of its children..
            if (parentID == folder_id)
                return true;

            // get parent and call recursively on the next level up parent id
            return CheckFolderForParentClash(uid, folder_id, _context.Users.Single(a => a.ID == uid).Folders.Single(b => b.ID == parentID).ParentID);
        }
    }
}
