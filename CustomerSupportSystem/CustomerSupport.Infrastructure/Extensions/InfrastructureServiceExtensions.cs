using CustomerSupport.Application.Interfaces.Authentication;
using CustomerSupport.Application.Interfaces.Files;
using CustomerSupport.Application.Interfaces.Repositories;
using CustomerSupport.Infrastructure.Authentication;
using CustomerSupport.Infrastructure.Data;
using CustomerSupport.Infrastructure.Files;
using CustomerSupport.Infrastructure.Repositories;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;

namespace CustomerSupport.Infrastructure.Extensions
{
    public static class InfrastructureServiceExtensions
    {
        public static IServiceCollection AddInfrastructureServices(
            this IServiceCollection services,
            string connectionString,
            string localAttachmentRootPath)
        {
            services.AddDbContext<CustomerSupportDbContext>(options =>
            {
                options.UseSqlServer(connectionString);
            });

            services.AddScoped<IUserRepository, UserRepository>();
            services.AddScoped<IRefreshTokenRepository, RefreshTokenRepository>();
            services.AddScoped<IProductRepository, ProductRepository>();
            services.AddScoped<ITicketCategoryRepository, TicketCategoryRepository>();
            services.AddScoped<ITicketPriorityRepository, TicketPriorityRepository>();
            services.AddScoped<ITicketStatusRepository, TicketStatusRepository>();
            services.AddScoped<ITicketRepository, TicketRepository>();

            services.AddScoped<IPasswordService, PasswordService>();
            services.AddScoped<ITokenService, JwtTokenService>();

            // Register Local file-storage settings as one application-wide object.
            services.AddSingleton(new LocalFileStorageOptions
            {
                RootPath = localAttachmentRootPath
            });

            // Register the current physical-storage implementation.
            // Later, this single registration can point to AzureBlobFileStorageService.
            services.AddScoped<IFileStorageService, LocalFileStorageService>();

            return services;
        }
    }
}