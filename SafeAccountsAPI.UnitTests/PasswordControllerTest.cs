using Microsoft.AspNetCore.Mvc;
using SafeAccountsAPI.Controllers;
using SafeAccountsAPI.Models;
using Xunit;

namespace SafeAccountsAPI.UnitTests
{
    public class PasswordControllerTest
    {
        [Fact]
        public void PasswordPost_Generates_Password_When_Default_Params_Are_Supplied()
        {
            var passwordController = new PasswordsController();
            var result = passwordController.GeneratePassword(new PasswordOptions());
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okObjectResult.StatusCode);
            Assert.False(okObjectResult.Value.ToString().Length == 0);
        }

        [Fact]
        public void PasswordPost_Generates_Password_When_Custom_Params_Are_Supplied()
        {
            var passwordController = new PasswordsController();

            var passwordOptions = new PasswordOptions() { MaxLength = 100, MinLength = 20 };
            var result = passwordController.GeneratePassword(passwordOptions);
            var okObjectResult = Assert.IsType<OkObjectResult>(result);
            Assert.Equal(200, okObjectResult.StatusCode);
            Assert.False(okObjectResult.Value.ToString().Length == 0);
        }
    }
}
