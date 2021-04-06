Use safeaccountsapi_db;

drop table accounts;
drop table refreshtokens;
drop table users;

Create Table Users
(
	ID int primary key auto_increment,
	First_Name nvarchar(20),
	Last_Name nvarchar(30),
	Email nvarchar(50) unique,
	Password varbinary(200),
	NumAccs int,
	Role nvarchar(25)
);

Create Table Accounts
(
	ID int primary key auto_increment,
	UserID int not null,
	Title nvarchar(50),
	Login nvarchar(50),
	Password nvarchar(50),
	Description nvarchar(250),
    CONSTRAINT FK_Accounts_UserID FOREIGN KEY (UserID)
    REFERENCES Users(ID)
);

Create Table RefreshTokens
(
	ID int primary key auto_increment,
	UserID int not null,
	Token nvarchar(100),
	Expiration nvarchar(150),
    CONSTRAINT FK_RefTokens_UserID FOREIGN KEY (UserID)
    REFERENCES Users(ID)
);