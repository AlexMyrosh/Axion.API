using System.Text.Json;
using System.Text.Json.Serialization;
using csharp_template.Auth.Abstraction;
using csharp_template.Auth.Implementation;
using csharp_template.Config;
using csharp_template.Config.Abstraction;
using csharp_template.DbRepositories.Abstraction;
using csharp_template.DbRepositories.Implementation;
using csharp_template.HealthCheckers.Abstraction;
using csharp_template.HealthCheckers.Implementation;
using csharp_template.HttpClient.Abstraction;
using csharp_template.HttpClient.Implementation;
using csharp_template.Middleware;
using csharp_template.Registry;
using csharp_template.SerilogConfiguration;
using csharp_template.Utilities;
using csharp_template.Validation;
using Serilog;
using Serilog.Context;
using Serilog.Events;

namespace csharp_template;

public abstract class Program
{
    public static async Task Main(string[] args)
    {
        try
        {
            Log.Information("Starting csharp_template application");

            var builder = WebApplication.CreateBuilder(args);

            // Add Serilog
            builder.Host.UseSerilog((_, _, configuration) => configuration
                .Enrich.FromLogContext()
                .MinimumLevel.Information()
                .MinimumLevel.Override("Microsoft.AspNetCore", LogEventLevel.Warning)
                .WriteTo.Console(formatter: new ConsoleFormatter()));
            
            // Adding ProcessId for initialization logs
            var processId = ProcessIdGenerator.Generate();
            LogContext.PushProperty("ProcessId", processId);

            // Config
            builder.Configuration.AddJsonFile("api_routes.json", optional: false, reloadOnChange: true);

            // Services
            builder.Services.AddSingleton<HandlerRegistry>();
            builder.Services.AddSingleton<IApiConfigurator, ApiConfigurator>();
            builder.Services.AddSingleton<IQueryConfigurator, QueryConfigurator>();
            builder.Services.AddSingleton<RequestValidator>();
            builder.Services.AddSingleton<ApiRouteValidator>();

            // Auth providers
            builder.Services.AddSingleton<IJwtAuthProvider, JwtAuthProvider>();
            builder.Services.AddSingleton<IStaticTokenAuthProvider, StaticTokenAuthProvider>();
            builder.Services.AddSingleton<JwtTokenGenerator>();
            
            // PostgreSQL service
            builder.Services.AddSingleton<IPostgresRepository, PostgresRepository>();
            
            // HTTP Client
            builder.Services.AddHttpClient();
            builder.Services.AddSingleton<IHttpClientWrapper, HttpClientWrapper>();
            
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
            app.UseMiddleware<RequestDataReadingMiddleware>(); // Read body once and cache it for better performance
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