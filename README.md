# SafeAccountsAPI (In Development)
The Open Source password manager API in the cloud. (RESTful implementation).<br />
<ins>Live Domain:</ins> https://eus-safeaccounts-test.azurewebsites.net

CI/CD is setup through Github Actions, so all accepted commits are available through the domain within about a minute, and SafeAccountsAPI is deployed as an Azure App Service that connects to an Azure Database for MySQL server.


<ins>**Important Note:**</ins> You cannot use the connection string to connect to the actual database in MySQL server, there is a whitelist of allowed IP addresses that can access it. That means if you wish to develop changes to the database structure, you must test them on your own SQL server first and then modify the corresponding TableCreateions.sql. Commits that are seen to change the DB layout in some fashion will be validated and then I will update the live MySQL server.

&nbsp;

## REST API Endpoints

To test the admin functionality on the live domain, I have a default admin account. <ins>Email: john@doe.com, Password: useless</ins>.<br />

Also, all inputs and outputs are in json format of course. <br />
Download [Postman](https://www.postman.com/downloads/) and use SafeAccountsAPI.postman_collection.json to easily test the API's endpoints.

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
* **/users/{id}/accounts**
  * Get - Get user's accounts. Admin or Authorized user only.
  * Post - Add a new account to the user. Admin or Authorized user only.
