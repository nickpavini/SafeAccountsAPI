Use SafeAccountsAPI_Db
Go

Create Table Users
(
	ID int IDENTITY(1,1) primary key,
	First_Name nvarchar(20),
	Last_Name nvarchar(30),
	Email nvarchar(50) unique,
	Password nvarchar(50),
	NumAccs int,
	Role nvarchar(25)
)

Create Table Accounts
(
	ID int IDENTITY(1,1) primary key,
	UserID int foreign key references Users(ID),
	Title nvarchar(50),
	Login nvarchar(50),
	Password nvarchar(50),
	Description nvarchar(250)
)