using Erasmus_SSC.Client.Services;
using Microsoft.AspNetCore.Components.WebAssembly.Hosting;
using Microsoft.AspNetCore.Components.Authorization;
using Erasmus_SSC.Services;
using Microsoft.Extensions.DependencyInjection;

namespace Erasmus_SSC.Client
{
    internal class Program
    {
        static async Task Main(string[] args)
        {
            var builder = WebAssemblyHostBuilder.CreateDefault(args);


            builder.Services.AddAuthorizationCore();
            builder.Services.AddScoped<ITokenStore, BrowserTokenStore>();
            builder.Services.AddScoped<JwtAuthStateProvider>();
            builder.Services.AddScoped<AuthenticationStateProvider>(sp => sp.GetRequiredService<JwtAuthStateProvider>());

            builder.Services.AddScoped(sp => new HttpClient
            {
                BaseAddress = new Uri(builder.HostEnvironment.BaseAddress)
            });



            builder.Services.AddScoped<AuthApiClient>();
            builder.Services.AddScoped<AdminUsersApiClient>();
            builder.Services.AddScoped<AdminNewsApiClient>();

            await builder.Build().RunAsync();
        }
    }
}
