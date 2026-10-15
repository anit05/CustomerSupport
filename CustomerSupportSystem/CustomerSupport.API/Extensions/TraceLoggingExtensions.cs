using Serilog.Context;

namespace CustomerSupport.API.Extensions
{
  // Creates an extension method for adding a custom middleware
  // that includes the ASP.NET Core TraceIdentifier in Serilog log entries.
  public static class TraceLoggingExtensions
  {
    // Extension method for IApplicationBuilder.
    // This allows us to register the middleware in Program.cs using:
    // app.UseTraceIdentifierLogging();
    public static IApplicationBuilder UseTraceIdentifierLogging(this IApplicationBuilder app)
    {
      // Register a custom middleware in the ASP.NET Core
      // HTTP request processing pipeline.
      return app.Use(async (context, next) =>
      {
        // 'context' represents the current HTTP request and response.
        // It is an HttpContext object containing information such as
        // Request, Response, User, Headers, TraceIdentifier, etc.

        // 'next' represents the next middleware in the request pipeline.
        // Calling next() passes the current HTTP request to the next middleware.

        // ASP.NET Core automatically assigns a unique TraceIdentifier
        // to every incoming HTTP request.
        // This identifier helps us correlate all log messages
        // generated while processing the same HTTP request.
        string traceIdentifier = context.TraceIdentifier;

        // Push the TraceIdentifier into Serilog's LogContext.
        // Once pushed, every Serilog log entry generated during
        // the processing of this request can automatically include
        // a property named "TraceIdentifier".
        using (LogContext.PushProperty("TraceIdentifier", traceIdentifier))
        {
          // Continue processing the HTTP request by invoking
          // the next middleware in the ASP.NET Core pipeline.
          await next();
        }
      });
    }
  }
}

