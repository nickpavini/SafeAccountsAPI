using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

using SafeAccountsAPI.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using System;
using System.Threading.Tasks;

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
            services.AddDbContext<APIContext>(options =>
                options.UseMySql(Configuration.GetValue<string>("ConnectionStrings:DefaultConnection"), ServerVersion.AutoDetect(Configuration.GetValue<string>("ConnectionStrings:DefaultConnection"))));

            services.AddCors(); // add cross-origin requests service

            services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
           .AddJwtBearer(options =>
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
                   IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(Configuration.GetValue<string>("JwtTokenKey")))
               };
               options.SaveToken = true;
               options.Events = new JwtBearerEvents
               {
                   OnMessageReceived = context =>
                   {
                       context.Token = context.Request.Cookies["AccessTokenSameSite"] ?? context.Request.Cookies["AccessToken"];
                       return Task.CompletedTask;
                   },
               };
           });

            services.AddHttpContextAccessor();
            services.AddControllers();
        }

        // This method gets called by the runtime. Use this method to configure the HTTP request pipeline.
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseHttpsRedirection();

            app.UseRouting();

            // allow from the locally hosted UI.. This should be seperate from our Host's (Azure app service) Enable cors area.
            // We need this because once the api is locked down, we need to be able to still test locally
            app.UseCors(options =>
            {
                options.WithOrigins("https://localhost:44325").AllowCredentials().AllowAnyHeader().AllowAnyMethod();
            });

            app.UseAuthentication();
            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
