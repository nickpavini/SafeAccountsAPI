# SafeAccountsAPI (In Development)
![example workflow](https://github.com/nickpavini/safeaccountsapi/actions/workflows/master_eus-safeaccounts-test.yml/badge.svg) <br />

The Open Source password manager API in the cloud. (RESTful implementation).<br />
<ins>Live Domain:</ins> https://eus-safeaccounts-test.azurewebsites.net

&nbsp;

<ins>**Security Comes First:**</ins> <br/>
I take security seriously and have implemented a 3 stage system that seperates the Database from the API and the UI's.<br/>
Everyone is welcome to test pentest the software so long as the intent is non-malicous and all bugs are reported.

&nbsp;

<ins>**Important Development Note:**</ins> <br/>
You must create your own local instance of MySql and update the connection string in `appsettings.development.json`. You can run `TableCreations_MySQL.sql` to setup your database and then it will populate with 5 users, 2 accounts and 2 folders each on first local run of the API.

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
* **/users/{id}**
  * Get - Retrieve user profile data. Admin or Authorized user only.
  * Delete - Remove a user and all associated data. Admin or Authorized user only
* **/users/{id}/firstname**
  * Put - Modify user first name. Admin or Authorized user only.
* **/users/{id}/lastname**
  * Put - Modify user last name. Admin or Authorized user only.
* **/users/{id}/password**
  * Put - Change user password. Admin or Authorized user only... Requires current password authentication.

&nbsp;

* **/users/{id}/accounts**
  * Get - Get user's accounts. Admin or Authorized user only.
  * Post - Add a new account to the user. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}**
  * Get - Get user's specific account. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}/title**
  * Put - Edit title of user's specific account. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}/login**
  * Put - Edit login for user's specific account. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}/password**
  * Put - Edit password of user's specific account. Admin or Authorized user only.
* **/users/{id}/accounts/{account_id}/description**
  * Put - Edit description of user's specific account. Admin or Authorized user only.

&nbsp;

* **/users/{id}/folders**
  * Get - Get user's folders. Admin or Authorized user only.
  * Post - Add a new folder to the user's profile. Admin or Authorized user only.
 
&nbsp;

* **/passwords/generate**
  * Post - Generate a password based on regex string.

&nbsp;

* **/refresh**
  * Post - Generate a new access token from refresh token and expired access token.
