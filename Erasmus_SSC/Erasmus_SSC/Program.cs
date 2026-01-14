using API.Services;
using Erasmus_SSC.Client.Pages;
using Erasmus_SSC.Components;
using Erasmus_SSC.Data;
using Erasmus_SSC.Interfaces;
using Erasmus_SSC.Models;
using Erasmus_SSC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Erasmus_SSC
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);


            builder.Services.AddDbContext<ApplicationDbContext>(options =>
         options
             .UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection"))
             .UseSnakeCaseNamingConvention()
            );

            builder.Services.AddScoped<IAuthService, AuthService>();

            builder.Services.AddScoped<IJWTService, JWTService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddLogging();
            builder.Services.AddScoped<ILoginAttemptService, LoginAttemptService>();
            builder.Services.AddControllers();


            builder.Services.AddEndpointsApiExplorer();
            builder.Services.AddSwaggerGen();
            builder.Services.AddAntiforgery(options =>
            {
                options.HeaderName = "X-XSRF-TOKEN";
            });


            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents()
                .AddInteractiveWebAssemblyComponents();


            var jwtSection = builder.Configuration.GetSection("Jwt");
            var keyString = jwtSection["Key"];

            if (string.IsNullOrWhiteSpace(keyString))
            {
                throw new InvalidOperationException("Jwt:Key is missing in appsettings.json");
            }

            var key = Encoding.UTF8.GetBytes(keyString);

            builder.Services
                .AddAuthentication(options =>
                {
                    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
                    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
                })
                .AddJwtBearer(options =>
                {
                    options.RequireHttpsMetadata = true;
                    options.SaveToken = true;
                    options.TokenValidationParameters = new TokenValidationParameters
                    {
                        ValidateIssuer = true,
                        ValidateAudience = true,
                        ValidateLifetime = true,
                        ValidateIssuerSigningKey = true,
                        ValidIssuer = jwtSection["Issuer"],
                        ValidAudience = jwtSection["Audience"],
                        IssuerSigningKey = new SymmetricSecurityKey(key),
                        ClockSkew = TimeSpan.Zero
                    };
                });
            builder.Services.AddScoped<IUserAdminService, UserAdminService>();

            builder.Services.AddAuthorization(options =>
            {
                options.AddPolicy("AdminOnly", policy =>
                    policy.RequireClaim("role", "Admin"));
            });

            builder.Services.AddMemoryCache();


            var app = builder.Build();

            // Seed test user password in development
            if (app.Environment.IsDevelopment())
            {
                using var scope = app.Services.CreateScope();
                var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

                var user = db.Users.FirstOrDefault(u => u.UserName == "test");
                if (user != null)
                {
                    var hasher = new PasswordHasher<User>();
                    user.PasswordHash = hasher.HashPassword(user, "Test12345");
                    db.SaveChanges();
                }


                if (app.Environment.IsDevelopment())
                {
                    app.UseWebAssemblyDebugging();
                    app.UseSwagger();
                    app.UseSwaggerUI();
                }
                else
                {
                    app.UseExceptionHandler("/Error");
                    app.UseHsts();
                }

                app.UseHttpsRedirection();
                app.UseStaticFiles();

                app.UseRouting();

                app.UseAuthentication();
                app.UseAuthorization();
                app.UseAntiforgery();
                app.MapControllers();

                app.MapStaticAssets();
                app.MapRazorComponents<App>()
                    .AddInteractiveServerRenderMode()
                    .AddInteractiveWebAssemblyRenderMode()
                    .AddAdditionalAssemblies(typeof(Client._Imports).Assembly);

                app.Run();
            }
        }
    }
}
