using Axion.API.Config;
using Axion.API.HealthCheckers.Abstraction;
using Axion.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Axion.API.Controllers;

[ApiController]
[Route("api/")]
public class HealthCheckController(ApiConfigurator apiConfigurator, IPostgresHealthCheck postgresHealthCheck, ILogger<HealthCheckController> logger) : ControllerBase
{
    [HttpGet("health_check")]
    public async Task<IActionResult> Check()
    {
        var healthStatus = new HealthCheckResult
        {
            ApiConfigurator = apiConfigurator.IsReady,
            Postgres = await postgresHealthCheck.CheckHealthAsync(),
        };

        var isHealthy = healthStatus is { ApiConfigurator: true, Postgres: true};
        if (isHealthy)
        {
            logger.LogInformation("HealthCheck: OK - All components are healthy. Components: {@Components}", healthStatus);
            return Ok(new { status = "OK", message = "Services are healthy" });
        }

        logger.LogWarning("HealthCheck: FAILED - Some components are not healthy. Components: {@Components}", healthStatus);
        return StatusCode(418, new { status = "not_ready", message = "Services are not ready" });
    }
}