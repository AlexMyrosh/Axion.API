using System.Text.Json;
using System.Text.Json.Serialization;
using Axion.API.Auth.Abstraction;
using Axion.API.Auth.Implementation;
using Axion.API.Config;
using Axion.API.Config.Abstraction;
using Axion.API.DbRepositories.Abstraction;
using Axion.API.DbRepositories.Implementation;
using Axion.API.HealthCheckers.Abstraction;
using Axion.API.HealthCheckers.Implementation;
using Axion.API.Middleware;
using Axion.API.Registry;
using Axion.API.SerilogConfiguration;
using Axion.API.Validation;
using Serilog;
using Serilog.Events;

namespace Axion.API;

public abstract class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            Log.Information("Starting Axion.API application");

            var builder = WebApplication.CreateBuilder(args);

            // Add Serilog
            builder.Host.UseSerilog((_, _, configuration) => configuration
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .WriteTo.Console(formatter: new ConsoleFormatter()));

            // Config
            builder.Configuration.AddJsonFile("api_routes.json", optional: false, reloadOnChange: true);

            // Services
            builder.Services.AddSingleton<HandlerRegistry>();
            builder.Services.AddSingleton<IApiConfigurator, ApiConfigurator>();
            builder.Services.AddSingleton<IQueryConfigurator, QueryConfigurator>();
            builder.Services.AddSingleton<RequestValidator>();

            // Auth providers
            builder.Services.AddSingleton<IJwtAuthProvider, JwtAuthProvider>();
            builder.Services.AddSingleton<IStaticTokenAuthProvider, StaticTokenAuthProvider>();
            
            // PostgreSQL service
            builder.Services.AddSingleton<IPostgresRepository, PostgresRepository>();
            
            // Health checks
            builder.Services.AddSingleton<IPostgresHealthCheck, PostgresHealthCheck>();
            builder.Services.AddSingleton<IRedisHealthCheck, RedisHealthCheck>();
            builder.Services.AddSingleton<IOracleHealthCheck, OracleHealthCheck>();
            builder.Services.AddSingleton<IKafkaHealthCheck, KafkaHealthCheck>();

            builder.Services.AddControllers().AddJsonOptions(o => { 
                o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
                o.JsonSerializerOptions.DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull;
            });

            var app = builder.Build();

            // Middleware pipeline
            app.UseMiddleware<ProcessIdMiddleware>(); // Should be always first in the pipeline
            app.UseMiddleware<GlobalExceptionMiddleware>();
            app.UseMiddleware<RequestLoggingMiddleware>();
            app.UseMiddleware<AuthMiddleware>();
            app.UseMiddleware<ValidationMiddleware>();

            if (app.Environment.IsDevelopment())
            {
                app.MapOpenApi();
            }

            app.MapControllers();

            // Load queries from /Queries directory
            var queryRegistry = app.Services.GetRequiredService<IQueryConfigurator>();
            await queryRegistry.InitializeAsync();
            
            // Initialize postgres service
            var postgresService = app.Services.GetRequiredService<IPostgresRepository>();
            await postgresService.InitializeAsync();

            // Load handlers from configuration
            var apiConfigurator = app.Services.GetRequiredService<IApiConfigurator>();
            await apiConfigurator.ConfigureAsync();

            await app.RunAsync();
        }
        catch (Exception ex)
        {
            Log.Fatal(ex, "Application terminated unexpectedly");
            throw;
        }
        finally
        {
            await Log.CloseAndFlushAsync();
        }
    }
}