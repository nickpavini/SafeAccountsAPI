using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using SafeAccountsAPI.Models;
using System.Threading.Tasks;

namespace SafeAccountsAPI.Data
{
    public class DbInitializer
    {
        public static void Initialize(APIContext context)
        {
            context.Database.EnsureCreated();

            // Look for any students.
            if (context.Users.Any())
            {
                return;   // DB has been seeded
            }

            // add base users if data base not populated
            var users = new User[]
            {
                new User { User_Name="johndoe", First_Name="John", Last_Name="Doe", Email="John@Doe.com", Password="useless", NumAccs=0 },
                new User { User_Name="johndoe", First_Name="John", Last_Name="Doe", Email="John@Doe.com", Password="useless", NumAccs=0 },
                new User { User_Name="johndoe", First_Name="John", Last_Name="Doe", Email="John@Doe.com", Password="useless", NumAccs=0 },
                new User { User_Name="johndoe", First_Name="John", Last_Name="Doe", Email="John@Doe.com", Password="useless", NumAccs=0 },
                new User { User_Name="johndoe", First_Name="John", Last_Name="Doe", Email="John@Doe.com", Password="useless", NumAccs=0}
            };

            foreach (User person in users) { context.Users.Add(person); } // add each user to the table
            context.SaveChanges(); // execute changes

            //This raw code isnt working for some reason with EnityFramework
            //context.Database.ExecuteSqlRaw(
            //        @"INSERT INTO TABLE Users (First_Name, Last_Name, Email, Password, NumAccs) Values (""Bob"", ""Jones"", ""Bob@Jones.com"", ""Useless"", 0)"
            //);
        }
    }
}
