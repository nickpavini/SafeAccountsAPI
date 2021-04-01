# SafeAccountsAPI
Cloud based account manager API (RESTful implementation).

&nbsp;


## REST API Endpoints
* **/users**
  * Get - Retrieve all users. Admin users only.
  * Post - Add new user.
* **/users/{id}**
  * Get - Retrieve user data. Admin or Authorized user only.
  * Delete - Remove a user and all associated data. Admin or Authorized user only
* **/users/{id}/firstname**
  * Put - Modify user first name. Admin or Authorized user only.
