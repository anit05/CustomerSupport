
using CustomerSupport.Mvc.Filters;
using CustomerSupport.Mvc.Interfaces.Services;
using CustomerSupport.Mvc.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.Mvc.Extensions
{
    public static class ServiceCollectionExtensions
    {
        public static IServiceCollection AddCustomerSupportMvc(
            this IServiceCollection services,
            IConfiguration configuration)
        {
            // Register the global API authentication filter.
            services.AddScoped<ApiAuthenticationFilter>();

            // Register MVC controllers and global filters.
            services.AddControllersWithViews(options =>
            {
                options.Filters.Add(
                    new AutoValidateAntiforgeryTokenAttribute());

                // Redirect to Login when API authentication is lost.
                options.Filters.AddService<ApiAuthenticationFilter>();
            });

            // Store API tokens in the server-side session.
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

            // Configure cookie authentication for MVC.
            services.AddAuthentication(
                CookieAuthenticationDefaults.AuthenticationScheme)
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

            // Configure the existing Web API HTTP client.
            services.AddHttpClient("CustomerSupportApi", client =>
            {
                string? baseUrl = configuration["WebApi:BaseUrl"];

                if (string.IsNullOrWhiteSpace(baseUrl) ||
                    !Uri.TryCreate(
                        baseUrl,
                        UriKind.Absolute,
                        out Uri? baseUri))
                {
                    throw new InvalidOperationException(
                        "Set a valid WebApi:BaseUrl in appsettings.json.");
                }

                client.BaseAddress =
                    baseUri.AbsoluteUri.EndsWith('/')
                        ? baseUri
                        : new Uri(baseUri.AbsoluteUri + "/");

                // Allow longer requests for AI processing.
                client.Timeout = TimeSpan.FromSeconds(
                    configuration.GetValue<int?>(
                        "WebApi:TimeoutSeconds") ?? 240);

                client.DefaultRequestHeaders.Accept.ParseAdd(
                    "application/json");
            });

            // Register the existing HTTP API client.
            services.AddScoped<IApiClient, ApiClient>();

            // Register existing feature services.
            services.AddScoped<IAuthenticationApi, AuthenticationApi>();
            services.AddScoped<ITicketsApi, TicketsApi>();
            services.AddScoped<ICatalogApi, CatalogApi>();
            services.AddScoped<IContentApi, ContentApi>();

            // Keep the previously added AI service registration.
            services.AddScoped<ITicketAIApi, TicketAIApi>();

            return services;
        }
    }
}
