using Microsoft.IdentityModel.Tokens;
using SafeAccountsAPI.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using SafeAccountsAPI.Data;
using System.Security.Cryptography;
using Newtonsoft.Json.Linq;
using Microsoft.AspNetCore.Http;
using System.Linq;

namespace SafeAccountsAPI.Controllers
{
    public static class HelperMethods
    {
        public static string token_key = "KeyForSignInSecret@1234"; // key for encrypting access tokens
        public static int salt_length = 12; // length of salts for password storage

        public static string GenerateJWTAccessToken(string role, string email)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(token_key));
            var signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            var tokeOptions = new JwtSecurityToken(
                issuer: "http://localhost:5000",
                audience: "http://localhost:5000",
                claims: new List<Claim> { new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.Email, email) },
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: signinCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokeOptions);
        }

        // generate our refresh token with expiration
        public static RefreshToken GenerateRefreshToken(User user, APIContext context)
        {
            // Create the refresh token
            RefreshToken refreshToken = new RefreshToken()
            {
                UserID = user.ID,
                Token = GenerateRefreshToken(),
                Expiration = DateTime.UtcNow.AddDays(1).ToString() // 1 day for reresh tokens
            };

            // Add it to the list of of refresh tokens for the user
            user.RefreshTokens.Add(refreshToken);

            // Update the user along with the new refresh token
            context.Update(user);
            return refreshToken;
        }

        // generate random base 64 string as token string
        public static string GenerateRefreshToken()
        {
            byte[] randomNumber = new byte[32];
            using (RandomNumberGenerator rng = RandomNumberGenerator.Create())
            {
                rng.GetBytes(randomNumber);
                return Convert.ToBase64String(randomNumber);
            }
        }

        // generate login response with valid access token, refresh token, and the id for user navigation
        public static string GenerateLoginResponse(string accessToken, RefreshToken rt, int id)
        {
            // format response
            JObject message = JObject.Parse(SuccessMessage._result);
            message.Add(new JProperty("token", accessToken));
            message.Add(new JProperty("refresh_token", JToken.FromObject(new ReturnableRefreshToken(rt))));
            message.Add(new JProperty("id", id));
            return message.ToString();
        }

        // make sure this user is either admin or trying to access something they own
        public static bool ValidateIsUserOrAdmin(IHttpContextAccessor httpContextAccessor, APIContext context, int id)
        {
            // verify that the user is either admin or is requesting their own data
            if (ValidateIsUser(httpContextAccessor, context, id) || ValidateIsAdmin(httpContextAccessor))
                return true;
            else
                return false;
        }

        // validate that this use is an Admin
        public static bool ValidateIsAdmin(IHttpContextAccessor httpContextAccessor)
        {
            string callerRole = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Role).Value;
            if (callerRole == UserRoles.Admin)
                return true;
            else
                return false;
        }

        // validate that the user trying to be accessed is the same as the user making the call
        public static bool ValidateIsUser(IHttpContextAccessor httpContextAccessor, APIContext context, int id)
        {
            string callerEmail = httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Email).Value;
            if (callerEmail == context.Users.Single(a => a.ID == id).Email)
                return true;
            else
                return false;
        }
        
        // create a salt for hashing
        public static byte[] CreateSalt(int size)
        {
            //Generate a cryptographic random number.
            RNGCryptoServiceProvider rng = new RNGCryptoServiceProvider();
            byte[] buff = new byte[size];
            rng.GetBytes(buff);

            // Return a Base64 string representation of the random number.
            return buff;
        }


        public static byte[] GenerateSaltedHash(byte[] plainText, byte[] salt)
        {
            HashAlgorithm algorithm = new SHA256Managed();


            byte[] plainTextWithSaltBytes =
              new byte[plainText.Length + salt.Length];

            // prepend salt
            for (int i = 0; i < salt.Length; i++)
            {
                plainTextWithSaltBytes[plainText.Length + i] = salt[i];
            }
            for (int i = 0; i < plainText.Length; i++)
            {
                plainTextWithSaltBytes[i] = plainText[i];
            }

            return algorithm.ComputeHash(plainTextWithSaltBytes);
        }
    }
}
