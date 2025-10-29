using Axion.API.Config;
using Axion.API.HealthCheckers.Abstraction;
using Axion.API.Models;
using Microsoft.AspNetCore.Mvc;

namespace Axion.API.Controllers;

[ApiController]
[Route("api-healthcheck")]
public class HealthCheckController(ApiConfigurator apiConfigurator, IPostgresHealthCheck postgresHealthCheck, IRedisHealthCheck redisHealthCheck, 
    IOracleHealthCheck oracleHealthCheck, IKafkaHealthCheck kafkaHealthCheck, ILogger<HealthCheckController> logger) : ControllerBase
{
    [HttpGet]
    public async Task<IActionResult> Check()
    {
        var healthStatus = new HealthCheckResult
        {
            ApiConfigurator = apiConfigurator.IsReady,
            
            // Check all database connections
            Postgres = await postgresHealthCheck.CheckHealthAsync(),
            Redis = await redisHealthCheck.CheckHealthAsync(),
            Oracle = await oracleHealthCheck.CheckHealthAsync(),
            Kafka = await kafkaHealthCheck.CheckHealthAsync()
        };

        var isHealthy = healthStatus is { ApiConfigurator: true, Postgres: true, Redis: true, Oracle: true, Kafka: true };
        if (isHealthy)
        {
            logger.LogInformation("HealthCheck: OK - All components are healthy");
            return Ok(new { status = "OK", message = "Services are healthy", components = healthStatus });
        }

        logger.LogWarning("HealthCheck: FAILED - Some components are not healthy. Components: {@Components}", healthStatus);
        return StatusCode(418, new { status = "not_ready", message = "Services are not ready", components = healthStatus });
    }
}