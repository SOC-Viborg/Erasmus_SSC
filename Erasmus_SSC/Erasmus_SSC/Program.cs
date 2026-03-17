using API.Services;
using Erasmus_SSC.Components;
using Erasmus_SSC.Data;
using Erasmus_SSC.Interfaces;
using Erasmus_SSC.Models;
using Erasmus_SSC.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using System.Text;

namespace Erasmus_SSC;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

       
        var conn = builder.Configuration.GetConnectionString("DefaultConnection");
        if (string.IsNullOrWhiteSpace(conn))
        {
            throw new InvalidOperationException(
                "ConnectionStrings:DefaultConnection is missing. " +
                "Check appsettings.json / appsettings.Development.json and also Visual Studio 'Manage User Secrets' (it can override config in Development)."
            );
        }

        builder.Services.AddDbContext<ApplicationDbContext>(options =>
            options
                .UseNpgsql(conn, npgsql => npgsql.EnableRetryOnFailure())
                .UseSnakeCaseNamingConvention());

       
        builder.Services.AddScoped<IAuthService, AuthService>();
        builder.Services.AddScoped<IUserAdminService, UserAdminService>();
        builder.Services.AddScoped<IJWTService, JWTService>();
        builder.Services.AddScoped<ILoginAttemptService, LoginAttemptService>();
       
        builder.Services.AddHttpContextAccessor();

        builder.Services.AddControllers();
        builder.Services.AddEndpointsApiExplorer();
        builder.Services.AddMemoryCache();

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
            throw new InvalidOperationException("Jwt:Key is missing. Check appsettings / user-secrets / env vars.");

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
       
        builder.Services.AddCascadingAuthenticationState();

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

      
        await ApplyMigrationsAndSeedAsync(app);

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

    private static async Task ApplyMigrationsAndSeedAsync(WebApplication app)
    {
        using var scope = app.Services.CreateScope();
        var db = scope.ServiceProvider.GetRequiredService<ApplicationDbContext>();

       
        await db.Database.MigrateAsync();

       
        if (!app.Environment.IsDevelopment())
            return;

        
        if (!await db.UserRoles.AnyAsync(r => r.Id == 1))
            db.UserRoles.Add(new UserRole { Id = 1, RoleName = "Admin" });
        if (!await db.UserRoles.AnyAsync(r => r.Id == 2))
            db.UserRoles.Add(new UserRole { Id = 2, RoleName = "User" });
        await db.SaveChangesAsync();

        
        var hasAdmin = await db.Users.AnyAsync(u => u.RoleId == 1);
        if (hasAdmin)
            return;

        const string adminEmail = "admin@local.dev";
        var emailExists = await db.Users.AnyAsync(u => u.Email.ToLower() == adminEmail);
        if (emailExists)
            return;

        var admin = new User
        {
            UserName = "admin",
            Email = adminEmail,
            RoleId = 1
        };

        var hasher = new PasswordHasher<User>();
        admin.PasswordHash = hasher.HashPassword(admin, "Admin123!");

        db.Users.Add(admin);
        await db.SaveChangesAsync();

        // Seed news from wwwroot/data/news.json if DB is empty
        if (!await db.News.AnyAsync())
        {
            var env = scope.ServiceProvider.GetRequiredService<IWebHostEnvironment>();
            var path = Path.Combine(env.WebRootPath, "data", "news.json");

            if (File.Exists(path))
            {
                var json = await File.ReadAllTextAsync(path);
                var seed = System.Text.Json.JsonSerializer.Deserialize<List<Erasmus_SSC.Dtos.News.PublicNewsDto>>(
                    json,
                    new System.Text.Json.JsonSerializerOptions { PropertyNameCaseInsensitive = true }
                ) ?? new();

                foreach (var n in seed)
                {
                    db.News.Add(new Erasmus_SSC.Models.News
                    {
                        Title = n.Title,
                        Description = n.Description,
                        ImageUrl = string.IsNullOrWhiteSpace(n.ImageUrl) ? null : n.ImageUrl,
                        PublishedAt = DateTime.SpecifyKind(n.Date, DateTimeKind.Utc)
                    });
                }

                await db.SaveChangesAsync();
            }
        }

    }

}
