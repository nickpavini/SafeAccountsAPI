using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using Newtonsoft.Json.Linq;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using System;
using System.Linq;
using System.Text;
using Microsoft.Extensions.Configuration;
using System.Collections.Generic;

namespace SafeAccountsAPI.Controllers {
	[ApiController]
	[Authorize]
	[Route("[controller]")]
	public class UsersController : Controller {
		private readonly APIContext _context; // database handle
		private readonly IHttpContextAccessor _httpContextAccessor; // handle to all http information.. used for authorization
		public IConfiguration _configuration; //

		// get an instance of a database and http handle
		public UsersController(APIContext context, IHttpContextAccessor httpContextAccessor, IConfiguration configuration) {
			_context = context;
			_httpContextAccessor = httpContextAccessor;
			_configuration = configuration;
		}

		// login and get tokens...
		[HttpPost("login"), AllowAnonymous] //working
		public string User_Login([FromBody] string credentials) {
			JObject json = null;
			try { json = JObject.Parse(credentials); } catch (Exception ex) {
				Response.StatusCode = 400;
				ErrorMessage error = new ErrorMessage("Invalid Json", credentials, ex.Message);
				return JObject.FromObject(error).ToString();
			}

			try {
				// get users saved password hash and salt
				User user = _context.Users.Single(a => a.Email == json["email"].ToString());

				// successful login.. compare user hash to the hash generated from the inputted password and salt
				if (ValidatePassword(json["password"].ToString(), user.Password)) {
					string tokenString = HelperMethods.GenerateJWTAccessToken(user.Role, user.Email, _configuration.GetValue<string>("JwtTokenKey"));
					RefreshToken refToken = HelperMethods.GenerateRefreshToken(user, _context);
					string ret = HelperMethods.GenerateLoginResponse(tokenString, refToken, user.ID);
					_context.SaveChanges(); // always last on db to make sure nothing breaks and db has new info

					// append cookies to response after login
					HelperMethods.SetCookies(Response, tokenString, refToken);
					return ret;
				} else {
					Response.StatusCode = 401;
					ErrorMessage error = new ErrorMessage("Invalid Credentials", credentials, Unauthorized().ToString());
					return JObject.FromObject(error).ToString();
				}
			} catch (Exception ex) {
				Response.StatusCode = 500; // later we will add logic to see if the error comes from users not giving all json arguments
				ErrorMessage error = new ErrorMessage("Error validating credentials", credentials, ex.Message);
				return JObject.FromObject(error).ToString();
			}
		}

		// compare string input to store hash and salt combo
		private bool ValidatePassword(string input, byte[] storedPassword) {
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

		// Get all available users.. might change later as it might not make sense to grab all accounts if there are tons
		[HttpGet] //working
		public string GetAllUsers() {
			if (!HelperMethods.ValidateIsAdmin(_httpContextAccessor)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid Role", "n/a", "Caller must have admin role.")).ToString(); // n/a for no args there
			}

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
		public string User_AddUser([FromBody] string userJson) {
			JObject json = null;

			// might want Json verification as own function since all will do it.. we will see
			try { json = JObject.Parse(userJson); } catch (Exception ex) {
				Response.StatusCode = 400;
				ErrorMessage error = new ErrorMessage("Invalid Json", userJson, ex.Message);
				return JObject.FromObject(error).ToString();
			}

			// attempt to create new user and add to the database... later we need to implement hashing
			try {
				User newUser = new User { First_Name = json["firstname"].ToString(), Last_Name = json["lastname"].ToString(), Email = json["email"].ToString(), Password = HelperMethods.ConcatenatedSaltAndSaltedHash(json["password"].ToString()), NumAccs = 0, Role = UserRoles.User };
				_context.Users.Add(newUser);
				_context.SaveChanges();
				HelperMethods.CreateUserKeyandIV(_context.Users.Single(a => a.Email == json["email"].ToString()).ID); // after we save changes, we need to get the user by their email and then use the id to create unique password and iv
			} catch (Exception ex) {
				Response.StatusCode = 500;
				ErrorMessage error = new ErrorMessage("Failed to create new user", json.ToString(), ex.Message);
				return JObject.FromObject(error).ToString();
			}

			JObject message = JObject.Parse(SuccessMessage._result);
			message.Add(new JProperty("id", _context.Users.Single(a => a.Email == json["email"].ToString()).ID)); // user context to get id since locally created user will not have id set
			return message.ToString();
		}

		// Get a specific user.
		[HttpGet("{id:int}")] // working
		public string User_GetUser(int id) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			//format response
			JObject message = JObject.Parse(SuccessMessage._result);
			ReturnableUser retUser = new ReturnableUser(_context.Users.Where(a => a.ID == id).Single()); // strips out private data that is never to be sent back
			message.Add(new JProperty("user", JToken.FromObject(retUser)));
			return message.ToString();
		}

		[HttpDelete("{id:int}")] // working
		public string User_DeleteUser(int id) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			try {
				// attempt to remove all data and update changes
				_context.Accounts.RemoveRange(_context.Accounts.Where(a => a.UserID == id));
				_context.RefreshTokens.RemoveRange(_context.RefreshTokens.Where(a => a.UserID == id));
				_context.Users.Remove(_context.Users.Single(a => a.ID == id));
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				ErrorMessage error = new ErrorMessage("Failed to delete user.", "ID: " + id.ToString(), ex.Message);
				return JObject.FromObject(error).ToString();
			}

			JObject message = JObject.Parse(SuccessMessage._result);
			return message.ToString();
		}

		[HttpGet("{id:int}/firstname")] // working
		public string User_GetFirstName(int id) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			JObject message = JObject.Parse(SuccessMessage._result);
			message.Add(new JProperty("firstname", _context.Users.Where(a => a.ID == id).Single().First_Name));
			return message.ToString();
		}

		[HttpPut("{id:int}/firstname")] // working
		public string User_EditFirstName(int id, [FromBody] string firstname) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			try {
				_context.Users.Where(a => a.ID == id).Single().First_Name = firstname;
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				ErrorMessage error = new ErrorMessage("Failed to update first name.", "ID: " + id.ToString() + " First Name: " + firstname, ex.Message);
				return JObject.FromObject(error).ToString();
			}

			JObject message = JObject.Parse(SuccessMessage._result);
			message.Add(new JProperty("new_firstname", _context.Users.Where(a => a.ID == id).Single().First_Name)); // this part re-affirms that in the database we have a new firstname
			return message.ToString();
		}

		[HttpGet("{id:int}/lastname")] // working
		public string User_GetLastName(int id) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			JObject message = JObject.Parse(SuccessMessage._result);
			message.Add(new JProperty("lastname", _context.Users.Where(a => a.ID == id).Single().Last_Name));
			return message.ToString();
		}

		[HttpPut("{id:int}/lastname")] // working
		public string User_EditLastName(int id, [FromBody] string lastname) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			try {
				_context.Users.Where(a => a.ID == id).Single().Last_Name = lastname;
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				ErrorMessage error = new ErrorMessage("Failed to update last name.", "ID: " + id.ToString() + " Last Name: " + lastname, ex.Message);
				return JObject.FromObject(error).ToString();
			}

			JObject message = JObject.Parse(SuccessMessage._result);
			message.Add(new JProperty("new_lastname", _context.Users.Where(a => a.ID == id).Single().Last_Name)); // this part re-affirms that in the database we have a new firstname
			return message.ToString();
		}

		[HttpPut("{id:int}/password")]
		public string User_EditPassword(int id, [FromBody] string passwordJson) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			JObject json = null;

			// might want Json verification as own function since all will do it.. we will see
			try { json = JObject.Parse(passwordJson); } catch (Exception ex) {
				Response.StatusCode = 400;
				ErrorMessage error = new ErrorMessage("Invalid Json", passwordJson, ex.Message);
				return JObject.FromObject(error).ToString();
			}

			try {
				User user = _context.Users.Single(a => a.ID == id);

				// if password is valid then we change it and update db
				if (ValidatePassword(json["current_password"].ToString(), user.Password)) {
					user.Password = HelperMethods.ConcatenatedSaltAndSaltedHash(json["new_password"].ToString());
					_context.Update(user);
					_context.SaveChanges();
				} else {
					Response.StatusCode = 401;
					return JObject.FromObject(new ErrorMessage("Invalid Password", json["current_password"].ToString(), "n/a")).ToString();
				}
			} catch (Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Failed to update with new password", "n/a", ex.Message)).ToString(); // don't continue to send password back and forth in messages
			}


			return JObject.Parse(SuccessMessage._result).ToString();
		}

		// get all of the user's accounts
		[HttpGet("{id:int}/accounts")] // working
		public string User_GetAccounts(int id) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			// format success response.. maybe could be done better but not sure yet
			JObject message = JObject.Parse(SuccessMessage._result);
			JArray accs = new JArray();
			foreach (Account acc in _context.Users.Single(a => a.ID == id).Accounts) { accs.Add(JToken.FromObject(new ReturnableAccount(acc))); }
			message.Add(new JProperty("accounts", accs));
			return message.ToString();
		}

		// add account.. input format is json
		[HttpPost("{id:int}/accounts")] // working
		public string User_AddAccount(int id, [FromBody] string accJson) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			JObject json = null;

			// might want Json verification as own function since all will do it.. we will see
			try { json = JObject.Parse(accJson); } catch (Exception ex) {
				Response.StatusCode = 400;
				ErrorMessage error = new ErrorMessage("Invalid Json", accJson, ex.Message);
				return JObject.FromObject(error).ToString();
			}

			try {
				// if folder id is present, then use it, if not we use standard null for top parent
				int? folder_id;
				if (json["folder_id"] == null)
					folder_id = null;
				else {
					folder_id = _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == int.Parse(json["folder_id"].ToString())).ID; // makes sure folder exists and is owned by user
				}

				// use token in header to to 
				Account new_account = new Account {
					UserID = id,
					FolderID = folder_id,
					Title = json["account_title"]?.ToString(),
					Login = json["account_login"]?.ToString(),
					Password = json["account_password"] != null ? HelperMethods.EncryptStringToBytes_Aes(json["account_password"].ToString(), HelperMethods.GetUserKeyAndIV(id)) : null,
					Description = json["account_description"]?.ToString()
				};
				_context.Accounts.Add(new_account);
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Error creating new account.", accJson, ex.Message)).ToString();
			}

			return SuccessMessage._result;
		}

		[HttpDelete("{id:int}/accounts/{account_id:int}")] // working
		public string User_DeleteAccount(int id, int account_id)
		{
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			try {
				_context.Accounts.Remove(_context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id)); // fist match user id to ensure ownership
				_context.SaveChanges();
			}
			catch(Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Error deleting account.", "Account ID: " + account_id.ToString(), ex.Message)).ToString();
			}

			return SuccessMessage._result;
		}

		// get a specific accounts info
		[HttpGet("{id:int}/accounts/{account_id:int}")]
		public string User_GetSingleAccount(int id, int account_id) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			JObject message = JObject.Parse(SuccessMessage._result);
			message.Add(new JProperty("account", JObject.FromObject(new ReturnableAccount(_context.Accounts.Single(a => a.ID == account_id)))));
			return message.ToString();
		}

		// edit a specific accounts info
		[HttpPut("{id:int}/accounts/{account_id:int}/title")] // in progress
		public string User_EditAccountTitle(int id, int account_id, [FromBody] string title) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			// attempt to edit the title
			try {
				Account acc = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
				acc.Title = title;
				_context.Accounts.Update(acc);
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Error editing title", "Attempted title: " + title, ex.Message)).ToString();
			}

			return SuccessMessage._result;
		}

		// edit a specific accounts info
		[HttpPut("{id:int}/accounts/{account_id:int}/login")]
		public string User_EditAccountLogin(int id, int account_id, [FromBody] string login) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			// attempt to edit the login
			try {
				Account acc = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
				acc.Login = login;
				_context.Accounts.Update(acc);
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Error editing login", "Attempted login: " + login, ex.Message)).ToString();
			}

			return SuccessMessage._result;
		}

		// edit a specific accounts info
		[HttpPut("{id:int}/accounts/{account_id:int}/password")] // in progress
		public string User_EditAccountPassword(int id, int account_id, [FromBody] string password) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			try {
				Account acc = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
				acc.Password = HelperMethods.EncryptStringToBytes_Aes(password, HelperMethods.GetUserKeyAndIV(id)); // this logic will need to be changed to use a unique key
				_context.Accounts.Update(acc);
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Error editing password", "n/a", ex.Message)).ToString();
			}

			return SuccessMessage._result;
		}

		// edit a specific accounts info
		[HttpPut("{id:int}/accounts/{account_id:int}/description")]
		public string User_EditAccountDesc(int id, int account_id, [FromBody] string description) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			// attempt to edit the description
			try {
				Account acc = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);
				acc.Description = description;
				_context.Accounts.Update(acc);
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Error editing description", "Attempted description: " + description, ex.Message)).ToString();
			}

			return SuccessMessage._result;
		}

		[HttpPut("{id:int}/accounts/{account_id:int}/folder")]
		public string User_AccountSetFolder(int id, int account_id, [FromBody] string folder_id) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			// attempt to edit the description
			try {
				Account acc = _context.Users.Single(a => a.ID == id).Accounts.Single(b => b.ID == account_id);

				// left empty implies removing any associated folder
				if (string.IsNullOrWhiteSpace(folder_id))
					acc.FolderID = null;
				else { // here we have to validate that the user owns the folder
					acc.FolderID = _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == int.Parse(folder_id)).ID; // we code it like this to make sure that whatever folder we attempt exists and is owner by this user
				}

				_context.Accounts.Update(acc);
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Error settting folder", "Attempted folder id: " + folder_id, ex.Message)).ToString();
			}

			return SuccessMessage._result;
		}

		[HttpGet("{id:int}/folders")]
		public string User_GetFolders(int id) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			// format success response.. maybe could be done better but not sure yet
			JObject message = JObject.Parse(SuccessMessage._result);
			JArray folders = new JArray();
			foreach (Folder fold in _context.Users.Single(a => a.ID == id).Folders) { folders.Add(JToken.FromObject(new ReturnableFolder(fold))); }
			message.Add(new JProperty("folders", folders));
			return message.ToString();
		}

		[HttpPost("{id:int}/folders")] // working
		public string User_AddFolder(int id, [FromBody] string folderJson) {
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			JObject json = null;

			// might want Json verification as own function since all will do it.. we will see
			try { json = JObject.Parse(folderJson); } catch (Exception ex) {
				Response.StatusCode = 400;
				ErrorMessage error = new ErrorMessage("Invalid Json", folderJson, ex.Message);
				return JObject.FromObject(error).ToString();
			}

			try {
				int? pid = json["parent_id"]?.ToObject<int?>(); // parent id

				// if user doesnt own the parent or isnt currently admin, we throw error
				if (pid != null && _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == pid) == null && !HelperMethods.ValidateIsAdmin(_httpContextAccessor))
					throw new Exception("User must own the parent folder or be admin");

				// use token in header to to 
				Folder new_folder = new Folder { UserID = id, FolderName = json["folder_name"].ToString(), ParentID = pid };
				_context.Folders.Add(new_folder); // add new folder

				// only update parent if needed
				if (pid != null) {
					Folder parent_folder = _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == pid); // this makes sure that the parent folder is owned by our user
					parent_folder.HasChild = true;
					_context.Folders.Update(parent_folder); // register to parent that is now has at least 1 child
				}
				_context.SaveChanges();
			} catch (Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Error creating new folder.", folderJson, ex.Message)).ToString();
			}

			return SuccessMessage._result;
		}

		// delete a folder and all contents if it is not empty
		[HttpDelete("{id:int}/folders/{folder_id:int}")] // working
		public string User_DeleteFolder(int id, int folder_id)
		{
			// verify that the user is either admin or is requesting their own data
			if (!HelperMethods.ValidateIsUserOrAdmin(_httpContextAccessor, _context, id)) {
				Response.StatusCode = 401;
				return JObject.FromObject(new ErrorMessage("Invalid User", "id accessed: " + id.ToString(), "Caller can only access their information.")).ToString();
			}

			try {
				Folder folderToDelete = _context.Users.Single(a => a.ID == id).Folders.Single(b => b.ID == folder_id);
				// if this folder has children, then we need to call DeleteFolder on all children
				if (folderToDelete.HasChild) {
					List<Folder> folders = _context.Users.Single(a => a.ID == id).Folders.ToList<Folder>();
					foreach (Folder folder in folders) {
						if (folder.ParentID == folderToDelete.ID) {
							User_DeleteFolder(id, folder.ID); // recursive call to go down the tree and delete children
						}
					}
				}

				// delete the accounts in the folder
				List<Account> accounts = _context.Users.Single(a => a.ID == id).Accounts.ToList<Account>();
				foreach (Account account in accounts) {
					if (account.FolderID == folderToDelete.ID) {
						_context.Accounts.Remove(account); // no need to call User_DeleteAccount because identity and access token have already been verifies
					}
				}
				_context.SaveChanges(); // save the accounts being deleted
				_context.Folders.Remove(folderToDelete); // remove the folder
				_context.SaveChanges(); // save the folder being deleted.. must be done seperate because of foreign keys
			}
			catch (Exception ex) {
				Response.StatusCode = 500;
				return JObject.FromObject(new ErrorMessage("Error deleting folder.", "Folder ID: " + folder_id.ToString(), ex.Message)).ToString();
			}

			return SuccessMessage._result;
		}
	}
}
