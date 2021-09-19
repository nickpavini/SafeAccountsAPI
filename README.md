# SafeAccountsAPI
![example workflow](https://github.com/nickpavini/safeaccountsapi/actions/workflows/master_safeaccounts-api.yml/badge.svg) <br />

**Website:** https://safeaccounts.net <br/>
**Discord Server:** https://discord.gg/9gvH9YweJe 


## Build:
* Start MySQL locally or use the provided docker compose file by running `docker compose up`

* Use `SafeAccountsAPI/TableCreations_MySQL.sql` to build the database and set relationships.

* Now that your database is setup, update the connection string in `SafeAccountsAPI/appsettings.development.json`. In case the docker-compose.yml is being used remember to update the password in the  connection string .

* Finally, run the api. You will see that 5 new users have been added as well as some accounts and folders.

   **Note**: This project uses .Net 5.0 so make sure you have that downloaded and setup with your Visual Studio.

&nbsp;

## REST API Endpoints

To test the admin functionality, modify the role of one of the default users directly in the database.

Also, all inputs and outputs are in json format of course. <br />
Download [Postman](https://www.postman.com/downloads/) and use SafeAccountsAPI.postman_collection.json to easily test the API's endpoints.

&nbsp;

* **/users**
  * Get - Retrieve all users. Admin users only.
  * Post - Add new user.
* **/users/login**
  * Post - Sign in with user credentials and retrieve tokens.
* **/users/logout**
  * Post - Log user out. Authorized user only
* **/users/{id}**
  * Get - Retrieve user profile data. Admin or Authorized user only.
  * Delete - Remove a user and all associated data. Admin or Authorized user only
* **/users/{id}/firstname**
  * Put - Modify user first name. Admin or Authorized user only.
* **/users/{id}/lastname**
  * Put - Modify user last name. Admin or Authorized user only.

&nbsp;

* **/users/{id}/accounts**
  * Get - Get user's accounts. Admin or Authorized user only.
  * Post - Add a new account to the user. Admin or Authorized user only.
  * Delete - Delete multiple saved accounts at once. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}**
  * Get - Get user's specific account. Admin or Authorized user only.
  * Delete - Delete a user's specific account. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}/title**
  * Put - Edit title of user's specific account. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}/login**
  * Put - Edit login for user's specific account. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}/password**
  * Put - Edit password of user's specific account. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}/description**
  * Put - Edit description of user's specific account. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}/folder**
  * Put - Set the folder to associate an account with. Admin or Authorized user only.

&nbsp;

* **/users/{id}/folders**
  * Get - Get user's folders. Admin or Authorized user only.
  * Post - Add a new folder to the user's profile. Admin or Authorized user only.
* **/users/{id}/folders/{folder_id}**
  * Delete - Delete a folder and all contents. Admin or Authorized user only.

&nbsp;

* **/passwords/generate**
  * Post - Generate a password based on regex string.

&nbsp;

* **/refresh**
  * Post - Generate a new access token from refresh token and expired access token.
