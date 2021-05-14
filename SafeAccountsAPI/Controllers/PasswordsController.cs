using System;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.Mvc;
using SafeAccountsAPI.Models;
using Microsoft.AspNetCore.Routing;

namespace SafeAccountsAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PasswordsController : ControllerBase
    {
        /*
         *  Json Parameters (in any order):
         *      RegexPatter: string // use to specify formatting of string
         *      MinLength: int
         *      MaxLength: int
         */
        [HttpPost("generate")]
        public IActionResult GeneratePassword([FromBody] PasswordOptions passwordOptions)
        {
            // check that the provided regex string was good
            Regex regex;
            try { regex = new Regex(passwordOptions.RegexPattern); } // try to create regex from the string
            catch (Exception ex)
            {
                ErrorMessage error = new ErrorMessage("Invalid Regex", ex.Message);
                return new BadRequestObjectResult(error);
            }

            string allChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789~`!@#$%^&*()_-+={[}]|\\:;\"'<,>.?/"; // string containing all possible chars

            // subtract 2000 date from current date and use as seed for better psuedo random numbers
            int seed = (DateTime.Now.Hour * 60 * 60 + DateTime.Now.Minute * 60 + DateTime.Now.Second) * 1000 + DateTime.Now.Millisecond; // get current milliseconds from midnight for seeding.. makes it much more random but still could be better

            Random randomNumGenerator = new Random(seed);
            int len = randomNumGenerator.Next(passwordOptions.MinLength, passwordOptions.MaxLength + 1);
            string password = ""; //empty password
            for (int i = 0; i < len; ++i)
            {
                // keep generating characters until they are within the regex specifications.. this logic works if we do not want them to specify placement or substrings
                char chr;
                do
                {
                    chr = allChars[randomNumGenerator.Next(0, allChars.Length)]; // place in all chars we are accessing
                } while (!regex.IsMatch(chr.ToString()));

                password += chr.ToString();
            }

            return Ok(password);
        }
    }
}
