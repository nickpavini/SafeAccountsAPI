using System;
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
using Konscious.Security.Cryptography;

namespace SafeAccountsAPI.Helpers
{
    public static class HelperMethods
    {
        public static int salt_length = 16; // length of salts for password storage

        // might want to combine JWT generation to a single function over time
        public static string GenerateJWTEmailConfirmationToken(int id, string token_key, string issAndAud)
        {
            SymmetricSecurityKey secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(token_key));
            SigningCredentials signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken tokeOptions = new JwtSecurityToken(
                issuer: issAndAud,
                audience: issAndAud,
                claims: new List<Claim> { new Claim(ClaimTypes.Actor, id.ToString()) },
                expires: DateTime.Now.AddDays(7), // 1 week to confirm
                signingCredentials: signinCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokeOptions);
        }

        public static string GenerateJWTAccessToken(int id, string token_key, string issAndAud)
        {
            SymmetricSecurityKey secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(token_key));
            SigningCredentials signinCredentials = new SigningCredentials(secretKey, SecurityAlgorithms.HmacSha256);

            JwtSecurityToken tokeOptions = new JwtSecurityToken(
                issuer: issAndAud,
                audience: issAndAud,
                claims: new List<Claim> { new Claim(ClaimTypes.Actor, id.ToString()), new Claim(ClaimTypes.Name, "access_token") },
                expires: DateTime.Now.AddMinutes(15), // these reset regularly
                signingCredentials: signinCredentials
            );

            return new JwtSecurityTokenHandler().WriteToken(tokeOptions);
        }

        public static User GetUserFromAccessToken(string accessToken, APIContext _context, string token_key, string issAndAud)
        {
            // paramters for a valid token.. might want to put in static class or function at some point
            TokenValidationParameters tokenValidationParamters = new TokenValidationParameters
            {
                ValidateIssuer = true,
                ValidateAudience = true,
                ValidateLifetime = false, // Do not validate lifetime here

                ValidIssuer = issAndAud,
                ValidAudience = issAndAud,
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

            // get the id from the token
            string id = principal.FindFirst(ClaimTypes.Actor)?.Value;
            if (string.IsNullOrEmpty(id))
            {
                throw new SecurityTokenException($"Missing claim: {ClaimTypes.Actor}!");
            }

            User user = _context.Users.Single(a => a.ID == int.Parse(id));
            return user;
        }

        // generate our refresh token with expiration
        public static RefreshToken GenerateRefreshToken(User user, APIContext context, string[] keyAndIv)
        {
            // Create the refresh token
            RefreshToken refreshToken = new RefreshToken()
            {
                UserID = user.ID,
                Token = HelperMethods.EncryptStringToBytes_Aes(GenerateRefreshToken(), keyAndIv),
                Expiration = HelperMethods.EncryptStringToBytes_Aes(DateTime.UtcNow.AddDays(200).ToString(), keyAndIv) // 200 days for reresh tokens
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
        public static bool ValidateRefreshToken(User user, string refreshToken, string[] keyAndIv)
        {
            if (user == null || !user.RefreshTokens.Exists(rt => rt.Token.SequenceEqual(HelperMethods.EncryptStringToBytes_Aes(refreshToken, keyAndIv))))
                return false;

            RefreshToken storedRefreshToken = user.RefreshTokens.Find(rt => rt.Token.SequenceEqual(HelperMethods.EncryptStringToBytes_Aes(refreshToken, keyAndIv)));

            // Ensure that the refresh token that we got from storage is not yet expired.
            if (DateTime.UtcNow > DateTime.Parse(HelperMethods.DecryptStringFromBytes_Aes(storedRefreshToken.Expiration, keyAndIv)))
                return false;

            return true;
        }

        // make sure this user is either admin or trying to access something they own
        public static bool ValidateIsUserOrAdmin(IHttpContextAccessor httpContextAccessor, APIContext context, int id, string[] keyAndIV)
        {
            // verify that the user is either admin or is requesting their own data
            if (ValidateIsUser(httpContextAccessor, id) || ValidateIsAdmin(context, id, keyAndIV))
                return true;
            else
                return false;
        }

        /* validate that this use is an Admin... here we dont check the token.
         * I think it is better to use the db because if the token is compromised, they still would have to have the id
         * of someone who is forsure an admin to get through this
         */
        public static bool ValidateIsAdmin(APIContext context, int id, string[] keyAndIV)
        {
            string callerRole = DecryptStringFromBytes_Aes(context.Users.Single(a => a.ID == id).Role, keyAndIV);
            return (callerRole == UserRoles.Admin) ? true : false;
        }

        // validate that the user trying to be accessed is the same as the user making the call
        public static bool ValidateIsUser(IHttpContextAccessor httpContextAccessor, int id)
        {
            int idFromToken = int.Parse(httpContextAccessor.HttpContext.User.FindFirst(ClaimTypes.Actor).Value);
            return (idFromToken == id) ? true : false;
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
            Argon2id hash = new Argon2id(plainText);
            hash.Salt = salt;
            hash.DegreeOfParallelism = 1;
            hash.Iterations = 2;
            hash.MemorySize = 16000; // 1 GB

            return hash.GetBytes(32); // 32 bytes == 256 bits .. 
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

        public static byte[] HexStringToByteArray(String hex)
        {
            int NumberChars = hex.Length;

            if (NumberChars % 2 != 0)
                throw new Exception("Not a hex string");

            byte[] bytes = new byte[NumberChars / 2];
            for (int i = 0; i < NumberChars; i += 2)
                bytes[i / 2] = Convert.ToByte(hex.Substring(i, 2), 16);
            return bytes;
        }

        public static CookieOptions GetCookieOptions(DateTime expiration, bool sameSite = false)
        {
            CookieOptions options = new CookieOptions
            {
                HttpOnly = true,
                Secure = true,
                Expires = expiration // set cookie to expire in 1 day
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
        public static LoggingInfo GetLoggingInfo(Exception ex, Guid correlationID) =>
            new LoggingInfo()
            {
                Exception = ex,
                HostName = Environment.MachineName,
                CorrelationID = correlationID
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
