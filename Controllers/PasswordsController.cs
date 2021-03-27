using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Cryptography;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json.Linq;

namespace SafeAccountsAPI.Controllers
{
    [Route("[controller]")]
    [ApiController]
    public class PasswordsController : ControllerBase
    {
        // GET: api/Passwords
        // generate a single password with the potential of applying regex standards in body
        //[HttpGet("generate")]
        //public string GeneratePassword()
        //{
        //    string options = @"{""regex"":""[a-zA-Z0-9]"",""minLength"":8,""maxLength"":12}"; // default options for a secure password that will fit any site.. 8-12 characters, capitals and numbers allowed
        //    return Generate(options);
        //}

        //// generate a password based on specific allowed characters in regex format
        [HttpGet("generate")]
        public string GeneratePassword([FromBody]string options = "")
        {
            /*
             * validate regex string here
             */
             if (options == "")
                options = @"{""regex"":""[a-zA-Z0-9]"",""minLength"":8,""maxLength"":12}"; // default options for a secure password that will fit any site.. 8-12 characters, capitals and numbers allowed

            return Generate(options);
        }

        // private function to generate passwords based on allowed expression
        /*
         *  Json Parameters (in any order):
         *      regex: string // use to specify formatting of string
         *      minLength: int
         *      maxLength: int
         */
        private string Generate(string options)
        {
            Regex regex = null;
            JObject json = null;

            // might want Json verification as own function since all will do it.. we will see
            try { json = JObject.Parse(options); }
            catch (Exception ex) {
                ErrorMessage error = new ErrorMessage("Invalid Json", options, ex.Message);
                return JObject.FromObject(error).ToString(); 
            }

            // check that the provided regex string was good
            try { regex = new Regex(json["regex"].ToString()); } // try to create regex from the string
            catch (Exception ex) {
                ErrorMessage error = new ErrorMessage("Invalid Json", json["regex"].ToString(), ex.Message);
                return JObject.FromObject(error).ToString();
            }

            string allChars = "abcdefghijklmnopqrstuvwxyzABCDEFGHIJKLMNOPQRSTUVWXYZ0123456789~`!@#$%^&*()_-+={[}]|\\:;\"'<,>.?/"; // string containing all possible chars
            
            // subtract 2000 date from current date and use as seed for better psuedo random numbers
            int seed = (DateTime.Now.Hour * 60 * 60 + DateTime.Now.Minute * 60 + DateTime.Now.Second) * 1000 + DateTime.Now.Millisecond; // get current milliseconds from midnight for seeding.. makes it much more random but still could be better

            Random randomNumGenerator = new Random(seed);
            int len = randomNumGenerator.Next(json["minLength"].ToObject<int>(), json["maxLength"].ToObject<int>() + 1);
            string password = ""; //empty password
            for(int i=0; i<len; ++i)
            {
                // keep generating characters until they are within the regex specifications.. this logic works if we do not want them to specify placement or substrings
                char chr;
                do
                {
                    chr = allChars[randomNumGenerator.Next(0, allChars.Length)]; // place in all chars we are accessing
                } while (!regex.IsMatch(chr.ToString()));

                password += chr.ToString();
            }

            // format success response.. maybe could be done better but not sure yet
            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("password", password));
            return message.ToString();
        }
    }
}
