using System;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.IdentityModel.Tokens;
using SafeAccountsAPI.Data;

namespace SafeAccountsAPI
{
    public class Startup
    {
        public IConfiguration Configuration { get; }

        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        // This method gets called by the runtime. Use this method to add services to the container.
        public void ConfigureServices(IServiceCollection services)
        {
            // probaby add exception logging somewhere here
            services.AddControllers().AddNewtonsoftJson();
            services.AddDbContext<APIContext>(options =>
                options.UseMySql(Configuration.GetValue<string>("ConnectionStrings:DefaultConnection"), ServerVersion.AutoDetect(Configuration.GetValue<string>("ConnectionStrings:DefaultConnection"))));

            services.AddCors(); // add cross-origin requests service

            // 2 forms of authentication
            // one is satisfied through user being logged in, the other is satisfied by giving an api key
            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
                .AddJwtBearer("ApiJwtToken", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.Zero, // use this to make time accurate when validating

                        ValidIssuer = "http://localhost:5000",
                        ValidAudience = "http://localhost:5000",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("ApiJwtTokenKey"))) // different password for the the api keys
                    };
                    options.SaveToken = true;
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            // get this token from authorization header
                            context.Token = context.Request.Headers.ContainsKey("ApiKey")
                                                ? context.Request.Headers["ApiKey"].ToString()
                                                : "";
                            return Task.CompletedTask;
                        },
                    };
                })
                .AddJwtBearer("UserJwtFromCookie", options =>
                {
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ClockSkew = TimeSpan.Zero, // use this to make time accurate when validating

                        ValidIssuer = "http://localhost:5000",
                        ValidAudience = "http://localhost:5000",
                        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("UserJwtTokenKey")))
                    };
                    options.Events = new JwtBearerEvents
                    {
                        OnMessageReceived = context =>
                        {
                            context.Token = context.Request.Cookies["AccessTokenSameSite"] ?? context.Request.Cookies["AccessToken"]; // get this token from cookies
                            return Task.CompletedTask;
                        },
                    };
                });

            // add authorization policies
            services.AddAuthorization(options =>
            {
                // must have an api key
                options.AddPolicy("ApiJwtToken", new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    // NOTE: Without this added claim, this policy always succeeds because the user is authenticated from the LoggedIn policy
                    // this makes us use the other jwt bearer option and check the claims in that token using the signing key for api_keys rather than for user jwt cookies
                    .RequireClaim(ClaimTypes.Name, "api_key")
                    .AddAuthenticationSchemes("ApiJwtToken")
                    .Build());

                // additional is that a user is signed in
                options.AddPolicy("LoggedIn", new AuthorizationPolicyBuilder()
                    .RequireAuthenticatedUser()
                    .RequireClaim(ClaimTypes.Name, "access_token")
                    .AddAuthenticationSchemes("UserJwtFromCookie")
                    .Build());
            });

            services.AddHttpContextAccessor();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env, IHostApplicationLifetime applicationLifetime)
        {
            app.UseHttpsRedirection();

            app.UseRouting();

            // allow from the locally hosted UI.. This should be seperate from our Host's (Azure app service) Enable cors area.
            // We need this because once the api is locked down, we need to be able to still test locally
            app.UseCors(options =>
            {
                options.WithOrigins(new string[] { "https://localhost:44325", "https://safeaccounts.azurewebsites.net" }).AllowCredentials().AllowAnyHeader().AllowAnyMethod();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });

            applicationLifetime.ApplicationStopping.Register(OnShutDown);
        }

        /// <summary>
        /// Add clean up code  
        /// </summary>
        private void OnShutDown()
        {
            // logger.flush()
            // Thread.sleep(1000);
        }
    }
}
