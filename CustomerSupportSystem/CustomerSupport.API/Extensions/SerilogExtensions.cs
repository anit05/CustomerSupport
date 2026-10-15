using Serilog;
namespace CustomerSupport.API.Extensions
{
  // Extension class used to configure Serilog logging
  // for the ASP.NET Core application.
  public static class SerilogExtensions
  {
    // Extension method for WebApplicationBuilder.
    // This allows us to configure Serilog using:
    //     builder.AddSerilogLogging();
    // from the Program.cs file.
    public static WebApplicationBuilder AddSerilogLogging(this WebApplicationBuilder builder)
    {
      // Configure Serilog as the logging provider for the ASP.NET Core host.

      // context    
      // Provides information about the application host.
      // It gives access to application configuration,
      // hosting environment, appsettings.json, environment variables, etc.

      // services     
      // Provides access to services registered in the
      // ASP.NET Core Dependency Injection container.
      // Serilog can use these registered services while configuring logging.

      // configuration  
      // Represents the Serilog LoggerConfiguration object.
      // We use this object to configure Serilog sources,
      // enrichers, sinks, log levels, output formats, etc.
      builder.Host.UseSerilog(
          (context, services, configuration) =>
          {
            configuration

                      // Read Serilog configuration from appsettings.json.
                      // This can include log levels, sinks,
                      // output templates, file locations, etc.
                      .ReadFrom.Configuration(context.Configuration)

                      // Allows Serilog to use services registered
                      // in the ASP.NET Core Dependency Injection container.
                      .ReadFrom.Services(services)

                      // Adds contextual information to log events.
                      // For example, properties added using LogContext
                      // can automatically become part of the log entry.
                      .Enrich.FromLogContext();
          });

      // Return the same builder so that method chaining
      // can continue in Program.cs.
      return builder;
    }
  }
}
