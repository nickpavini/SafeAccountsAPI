/*Create database safeaccountsapi_db;*/
Use safeaccountsapi_db;

drop table accounts;
drop table refreshtokens;
drop table folders;
drop table users;

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
    FolderName varbinary(200),
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
	Title varbinary(200),
	Login varbinary(200),
	Password varbinary(200),
    Url varbinary(200),
	Description varbinary(600),
    LastModified varbinary(200),
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