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

namespace SafeAccountsAPI.Controllers
{
    public static class HelperMethods
    {
        public static string GenerateJWTAccessToken(string role, string email)
        {
            var secretKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes("KeyForSignInSecret@1234"));
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
    }
}
