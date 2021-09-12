﻿using System.Net;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc.Testing;
using Newtonsoft.Json;
using SafeAccountsAPI.Models;
using Xunit;

namespace SafeAccountsAPI.UnitTests
{
    public class PasswordControllerIntegrationTest : IClassFixture<WebApplicationFactory<SafeAccountsAPI.Startup>>
    {
        public HttpClient _client { get; }
        public PasswordControllerIntegrationTest(WebApplicationFactory<SafeAccountsAPI.Startup> fixture)
        {
            _client = fixture.CreateClient();

            // set default header for our api_key... Development key only, doesnt work with online api
            _client.DefaultRequestHeaders.Add("ApiKey", "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9.eyJodHRwOi8vc2NoZW1hcy54bWxzb2FwLm9yZy93cy8yMDA1LzA1L2lkZW50aXR5L2NsYWltcy9uYW1lIjoiYXBpX2tleSIsImV4cCI6MTY2Mjk4NzA4MywiaXNzIjoiaHR0cHM6Ly9sb2NhbGhvc3Q6NDQzNjYiLCJhdWQiOiJodHRwczovL2xvY2FsaG9zdDo0NDM2NiJ9.NUf-fL3g72Z8XqihXJIuaG_z8_NEHmSwckb94VgVK3Q");
        }

        [Fact]
        public async Task POST_Should_Generate_Password_With_Default_Options()
        {
            /*
             * HttpPost("passwords/generate")
             */

            var passwordOptions = new PasswordOptions();
            var content = new StringContent(JsonConvert.SerializeObject(passwordOptions), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("passwords/generate", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }

        [Fact]
        public async Task POST_Should_Generate_Password_With_Custom_Options()
        {
            /*
             * HttpPost("passwords/generate")
             */

            var passwordOptions = new PasswordOptions() { MaxLength = 100, MinLength = 20 };
            var content = new StringContent(JsonConvert.SerializeObject(passwordOptions), Encoding.UTF8, "application/json");
            var response = await _client.PostAsync("passwords/generate", content);
            Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        }
    }
}
