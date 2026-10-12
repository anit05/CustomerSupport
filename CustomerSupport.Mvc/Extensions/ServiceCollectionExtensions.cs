using CustomerSupport.Mvc.Interfaces.Services;
using CustomerSupport.Mvc.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.Mvc.Extensions
{
    /// <summary>
    /// Registers MVC and API-integration dependencies in a single place.
    /// </summary>
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomerSupportMvc(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            services.AddControllersWithViews(options =>
                options.Filters.Add(new AutoValidateAntiforgeryTokenAttribute()));

            // Authentication tickets and API tokens are kept on the MVC server.
            // For a scaled production deployment, use distributed session storage
            // and a persistent/shared ASP.NET Core Data Protection key ring.
            services.AddDistributedMemoryCache();
            services.AddSession(options =>
            {
                options.IdleTimeout = TimeSpan.FromDays(7);
                options.Cookie.Name = ".SupportDesk.Session";
                options.Cookie.HttpOnly = true;
                options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                options.Cookie.SameSite = SameSiteMode.Lax;
                options.Cookie.IsEssential = true;
            });

            services.AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
                .AddCookie(options =>
                {
                    options.Cookie.Name = ".SupportDesk.Auth";
                    options.Cookie.HttpOnly = true;
                    options.Cookie.SecurePolicy = CookieSecurePolicy.Always;
                    options.Cookie.SameSite = SameSiteMode.Lax;
                    options.LoginPath = "/login";
                    options.AccessDeniedPath = "/access-denied";
                    options.ExpireTimeSpan = TimeSpan.FromDays(7);
                    options.SlidingExpiration = true;
                });

            services.AddAuthorization();
            services.AddHttpContextAccessor();

            // All service implementations use the same configured Web API base URL.
            services.AddHttpClient("CustomerSupportApi", client =>
            {
                var baseUrl = configuration["WebApi:BaseUrl"];
                if (string.IsNullOrWhiteSpace(baseUrl) ||
                    !Uri.TryCreate(baseUrl, UriKind.Absolute, out var baseUri))
                {
                    throw new InvalidOperationException(
                        "Set a valid WebApi:BaseUrl in appsettings.json.");
                }

                client.BaseAddress = baseUri.AbsoluteUri.EndsWith('/')
                    ? baseUri : new Uri(baseUri.AbsoluteUri + "/");
                client.Timeout = TimeSpan.FromSeconds(60);
                client.DefaultRequestHeaders.Accept.ParseAdd("application/json");
            });

            services.AddScoped<IApiClient, ApiClient>();
            services.AddScoped<IAuthenticationApi, AuthenticationApi>();
            services.AddScoped<ITicketsApi, TicketsApi>();
            services.AddScoped<ICatalogApi, CatalogApi>();
            services.AddScoped<IContentApi, ContentApi>();

            return services;
        }
    }
}
