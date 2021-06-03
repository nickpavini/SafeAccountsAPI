﻿using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.IO;
using System.Linq;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using Microsoft.AspNetCore.Http;
using Microsoft.IdentityModel.Tokens;
using SafeAccountsAPI.Data;
using SafeAccountsAPI.Models;
using SafeAccountsAPI.Logging;

namespace SafeAccountsAPI.Helpers
{
    public static class HelperMethods
    {
        private static readonly string keys_file = "keys.txt"; // file for securely storing user keys and ivs
        public static int salt_length = 16; // length of salts for password storage

        // might want to combine JWT generation to a single function over time
        public static string GenerateJWTEmailConfirmationToken(string email, string token_key)
        {
            SymmetricSecurityKey secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(token_key));
            SigningCredentials signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken tokeOptions = new JwtSecurityToken(
                issuer: "http://localhost:5000",
                audience: "http://localhost:5000",
                claims: new List<Claim> { new Claim(ClaimTypes.Email, email) },
                expires: DateTime.Now.AddDays(7), // 1 week to confirm
                signingCredentials: signinCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokeOptions);
        }

        public static string GenerateJWTAccessToken(string role, string email, string token_key)
        {
            SymmetricSecurityKey secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(token_key));
            SigningCredentials signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken tokeOptions = new JwtSecurityToken(
                issuer: "http://localhost:5000",
                audience: "http://localhost:5000",
                claims: new List<Claim> { new Claim(ClaimTypes.Role, role), new Claim(ClaimTypes.Email, email), new Claim(ClaimTypes.Name, "access_token") },
                expires: DateTime.Now.AddMinutes(15),
                signingCredentials: signinCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokeOptions);
        }

        public static User GetUserFromAccessToken(string accessToken, APIContext _context, string token_key)
        {
            // paramters for a valid token.. might want to put in static class or function at some point
            TokenValidationParameters tokenValidationParamters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Do not validate lifetime here

                ValidIssuer = "http://localhost:5000",
                ValidAudience = "http://localhost:5000",
                IssuerSigningKey =
                    new SymmetricSecurityKey(
                        Encoding.ASCII.GetBytes(token_key)
                    )
            };

            // validate the received token
            JwtSecurityTokenHandler tokenHandler = new JwtSecurityTokenHandler();
            ClaimsPrincipal principal = tokenHandler.ValidateToken(accessToken, tokenValidationParamters, out SecurityToken securityToken);
            JwtSecurityToken jwtSecurityToken = securityToken as JwtSecurityToken;
            if (jwtSecurityToken == null || !jwtSecurityToken.Header.Alg.Equals(SecurityAlgorithms.HmacSha256, StringComparison.InvariantCultureIgnoreCase))
            {
                throw new SecurityTokenException("Invalid token!");
            }

            // get the email from the token
            string email = principal.FindFirst(ClaimTypes.Email)?.Value;
            if (string.IsNullOrEmpty(email))
            {
                throw new SecurityTokenException($"Missing claim: {ClaimTypes.Email}!");
            }

            User user = _context.Users.Single(a => a.Email == email);
            return user;
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
            context.SaveChanges(); // save last
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

        // make sure the refresh token is valid
        public static bool ValidateRefreshToken(User user, string refreshToken)
        {
            if (user == null || !user.RefreshTokens.Exists(rt => rt.Token == refreshToken))
                return false;

            RefreshToken storedRefreshToken = user.RefreshTokens.Find(rt => rt.Token == refreshToken);

            // Ensure that the refresh token that we got from storage is not yet expired.
            if (DateTime.UtcNow > DateTime.Parse(storedRefreshToken.Expiration))
                return false;

            return true;
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

            // return byte array representing salt.
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

        public static byte[] ConcatenatedSaltAndSaltedHash(string passwordStr)
        {
            // hash password with salt.. still trying to understand a bit about the difference between unicode and base 64 string so for now we are just dealing with byte arrays
            byte[] salt = HelperMethods.CreateSalt(HelperMethods.salt_length);
            byte[] password = HelperMethods.GenerateSaltedHash(Encoding.UTF8.GetBytes(passwordStr), salt);
            byte[] concatenated = new byte[salt.Length + password.Length];
            Buffer.BlockCopy(salt, 0, concatenated, 0, salt.Length);
            Buffer.BlockCopy(password, 0, concatenated, salt.Length, password.Length);

            return concatenated;
        }

        // get the key associated to the specific user that is used to secure the user's saved passwords
        // password to the actual user's account isnt encrypted because it is hashed for matching.. might later want to encrypt to add extra layer against theft
        public static string[] GetUserKeyAndIV(int uid)
        {
            if (!File.Exists(keys_file))
                throw new Exception("File for keys does not exist.");

            string[] keyAndIv = null;
            foreach (string line in File.ReadAllLines(keys_file))
            {
                string[] line_split = line.Split(" "); // id key iv
                if (uid.ToString() == line_split[0])
                { // found matching id
                    keyAndIv = new string[2];
                    keyAndIv[0] = line_split[1]; // key
                    keyAndIv[1] = line_split[2]; // iv
                }
            }

            // return empty string if we never found the user
            if (keyAndIv == null)
                return keyAndIv;

            return keyAndIv;
        }

        // created when a new user signs up. This is their unique key and iv for securing the items in their safe
        public static string CreateUserKeyandIV(int uid)
        {
            if (!File.Exists(keys_file))
                using (File.Create(keys_file)) { }; // simply create the file and dispose of the file stream

            // if there is a key already we simply return the key
            if (GetUserKeyAndIV(uid) != null)
                throw new Exception("Key for user id already exists.");

            string key, iv;
            using (Aes myAes = Aes.Create())
            {
                key = Convert.ToBase64String(myAes.Key);
                iv = Convert.ToBase64String(myAes.IV);
            }

            File.AppendAllText(keys_file, uid.ToString() + " " + key + " " + iv + "\n"); // id key iv
            return key + "+" + iv;
        }

        public static byte[] EncryptStringToBytes_Aes(string plainText, string[] keyAndIv)
        {
            byte[] encrypted;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(keyAndIv[0]);
                aesAlg.IV = Convert.FromBase64String(keyAndIv[1]);

                // Create an encryptor to perform the stream transform.
                ICryptoTransform encryptor = aesAlg.CreateEncryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for encryption.
                using (MemoryStream msEncrypt = new MemoryStream())
                {
                    using (CryptoStream csEncrypt = new CryptoStream(msEncrypt, encryptor, CryptoStreamMode.Write))
                    {
                        using (StreamWriter swEncrypt = new StreamWriter(csEncrypt))
                        {
                            //Write all data to the stream.
                            swEncrypt.Write(plainText);
                        }
                        encrypted = msEncrypt.ToArray();
                    }
                }
            }

            // Return the encrypted bytes from the memory stream.
            return encrypted;
        }

        public static string DecryptStringFromBytes_Aes(byte[] cipherText, string[] keyAndIv)
        {
            // Declare the string used to hold
            // the decrypted text.
            string plaintext = null;

            // Create an Aes object
            // with the specified key and IV.
            using (Aes aesAlg = Aes.Create())
            {
                aesAlg.Key = Convert.FromBase64String(keyAndIv[0]);
                aesAlg.IV = Convert.FromBase64String(keyAndIv[1]);

                // Create a decryptor to perform the stream transform.
                ICryptoTransform decryptor = aesAlg.CreateDecryptor(aesAlg.Key, aesAlg.IV);

                // Create the streams used for decryption.
                using (MemoryStream msDecrypt = new MemoryStream(cipherText))
                {
                    using (CryptoStream csDecrypt = new CryptoStream(msDecrypt, decryptor, CryptoStreamMode.Read))
                    {
                        using (StreamReader srDecrypt = new StreamReader(csDecrypt))
                        {

                            // Read the decrypted bytes from the decrypting stream
                            // and place them in a string.
                            plaintext = srDecrypt.ReadToEnd();
                        }
                    }
                }
            }

            return plaintext;
        }

        public static void SetCookies(HttpResponse Response, string tokenString, RefreshToken refToken)
        {
            // append cookies after login
            Response.Cookies.Append("AccessToken", tokenString, HelperMethods.GetCookieOptions(false));
            Response.Cookies.Append("RefreshToken", refToken.Token, HelperMethods.GetCookieOptions(false));
            Response.Cookies.Append("AccessTokenSameSite", tokenString, HelperMethods.GetCookieOptions(true));
            Response.Cookies.Append("RefreshTokenSameSite", refToken.Token, HelperMethods.GetCookieOptions(true));
        }

        public static CookieOptions GetCookieOptions(bool sameSite = false)
        {
            CookieOptions options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = DateTime.UtcNow.AddDays(1) // set cookie to expire in 1 day
            };
            if (sameSite) options.SameSite = Microsoft.AspNetCore.Http.SameSiteMode.None; // for cross site requests
            return options;
        }

        /// <summary>
        /// 
        /// </summary>
        /// <param name="ex"></param>
        /// <returns></returns>
        /// <remarks>Add more fields for better analysis</remarks>
        public static LoggingInfo GetLoggingInfo(Exception ex) =>
            new LoggingInfo()
            {
                Exception = ex,
                HostName = Environment.MachineName
            };

        /// <summary>
        /// 
        /// </summary>
        /// <param name="operation"></param>
        /// <param name="routeValues"></param>
        /// <returns></returns>
        public static LoggingInfo GetLoggingInfo(string operation, IDictionary<string, object> routeValues)
        {
            var additionalInfo = new Dictionary<string, object>();
            foreach (var item in routeValues)
            {
                additionalInfo.Add(item.Key, item.Value);
            }
            return new LoggingInfo()
            {
                Operation = operation,
                HostName = Environment.MachineName,
                AdditionalInfo = additionalInfo
            };
        }

    }
}
