using CustomerSupport.Mvc.Extensions;

namespace CustomerSupport.Mvc
{
    /// <summary>
    /// Entry point for the ASP.NET Core MVC front-end.
    /// This application uses the CustomerSupport.API Web API for all business operations.
    /// </summary>
    public sealed class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            // Controllers, Razor views, authentication, session, and typed API services.
            builder.Services.AddCustomerSupportMvc(builder.Configuration);

            var app = builder.Build();

            ConfigureHttpPipeline(app);

            app.Run();
        }

        private static void ConfigureHttpPipeline(WebApplication app)
        {
            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/server-error");
                app.UseHsts();
            }

            app.UseHttpsRedirection();
            app.UseStatusCodePagesWithReExecute("/not-found");
            app.UseStaticFiles();
            app.UseRouting();
            app.UseSession();
            app.UseAuthentication();
            app.UseAuthorization();

            app.MapControllers();
            app.MapControllerRoute(
                name: "default",
                pattern: "{controller=Home}/{action=Index}/{id?}");
        }
    }
}
