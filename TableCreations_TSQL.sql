Use SafeAccountsAPI_Db
Go

Create Table Users
(
	ID int IDENTITY(1,1) primary key,
	First_Name nvarchar(20),
	Last_Name nvarchar(30),
	Email nvarchar(50) unique,
	Password varbinary(200),
	NumAccs int,
	Role nvarchar(25)
)

Create Table Accounts
(
	ID int IDENTITY(1,1) primary key,
	UserID int foreign key references Users(ID),
	Title nvarchar(50),
	Login nvarchar(50),
	Password varbinary(200),
	Description nvarchar(250)
)

Create Table RefreshTokens
(
	ID int IDENTITY(1,1) primary key,
	UserID int foreign key references Users(ID),
	Token nvarchar(100),
	Expiration nvarchar(150)
)