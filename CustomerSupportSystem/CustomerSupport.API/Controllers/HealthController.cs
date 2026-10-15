using CustomerSupport.Infrastructure.Data;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace CustomerSupport.API.Controllers
{
  [ApiController]
  [Route("health")]
  [AllowAnonymous]
  public class HealthController : ControllerBase
  {
    private readonly CustomerSupportDbContext _context;

    public HealthController(CustomerSupportDbContext context)
    {
      _context = context;
    }

    [HttpGet]
    public async Task<IActionResult> GetHealth()
    {
      // Check whether SQL Server is accessible.
      bool databaseConnected =
          await _context.Database.CanConnectAsync();

      if (!databaseConnected)
      {
        return StatusCode(503, new
        {
          status = "Unhealthy",
          database = "Disconnected"
        });
      }

      return Ok(new
      {
        status = "Healthy",
        database = "Connected"
      });
    }
  }
}
