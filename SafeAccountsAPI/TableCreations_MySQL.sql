/*Create database safeaccountsapi_db;*/
Use safeaccountsapi_db;

drop table if exists accounts;
drop table if exists refreshtokens;
drop table if exists folders;
drop table if exists users;

Create Table Users
(
	ID int primary key auto_increment,
	First_Name varbinary(80),
	Last_Name varbinary(80),
	Email varbinary(200) unique,
	Password varbinary(200),
	Role varbinary(80),
    EmailVerified bool not null
);

Create Table Folders
(
	ID int primary key auto_increment,
    UserID int not null,
    ParentID int,
    HasChild bool not null,
    FolderName varbinary(32), /*n-1 chars can be saved aes encrypted*/
    Constraint FK_Folders_UserID foreign key (UserID)
    references Users(ID),
    Constraint FK_Folders_ParentID foreign key (ParentID)
    references Folders(ID)
);

Create Table Accounts
(
	ID int primary key auto_increment,
	UserID int not null,
    FolderID int,
	Title varbinary(64), /*n-1 chars can be saved aes encrypted*/
	Login varbinary(48),
	Password varbinary(48),
    Url varbinary(192),
	Description varbinary(592),
    LastModified nvarchar(50), /*simple date string*/
    IsFavorite bool not null,
    CONSTRAINT FK_Accounts_UserID FOREIGN KEY (UserID)
    REFERENCES Users(ID),
    CONSTRAINT FK_Accounts_FolderID FOREIGN KEY (FolderID)
    REFERENCES Folders(ID)
);

Create Table RefreshTokens
(
	ID int primary key auto_increment,
	UserID int not null,
	Token varbinary(200),
	Expiration varbinary(100),
    CONSTRAINT FK_RefTokens_UserID FOREIGN KEY (UserID)
    REFERENCES Users(ID)
);

SELECT "table creation successful!";