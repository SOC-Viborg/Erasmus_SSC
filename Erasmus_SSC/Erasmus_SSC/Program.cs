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
            builder.Services.AddScoped<IUserAdminService, UserAdminService>();
            builder.Services.AddScoped<IJWTService, JWTService>();
            builder.Services.AddHttpContextAccessor();
            builder.Services.AddLogging();
            builder.Services.AddScoped<ILoginAttemptService, LoginAttemptService>();
            builder.Services.AddControllers();


            builder.Services.AddEndpointsApiExplorer();
           
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
            builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,

            ValidIssuer = jwtSection["Issuer"],
            ValidAudience = jwtSection["Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(keyString)),

            ClockSkew = TimeSpan.FromSeconds(30)
        };
    });

            builder.Services.AddAuthorization();

            var key = Encoding.UTF8.GetBytes(keyString);

            builder.Services.AddSwaggerGen(c =>
            {
                c.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
                {
                    Name = "Authorization",
                    Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
                    Scheme = "bearer",
                    BearerFormat = "JWT",
                    In = Microsoft.OpenApi.Models.ParameterLocation.Header,
                    Description = "Enter: Bearer {your JWT token}"
                });

                c.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
            });


            builder.Services.AddMemoryCache();


            var app = builder.Build();

         
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


