using System.Text.Json;
using System.Text.Json.Serialization;
using Axion.API.Auth.Abstraction;
using Axion.API.Auth.Implementation;
using Axion.API.Config;
using Axion.API.Middleware;
using Axion.API.Registry;
using Axion.API.SerilogConfiguration;
using Axion.API.Services.Abstraction;
using Axion.API.Services.Implementation;
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
            builder.Services.AddSingleton<ApiConfigurator>();
            builder.Services.AddSingleton<RequestValidator>();

            // Auth providers
            builder.Services.AddSingleton<IJwtAuthProvider, JwtAuthProvider>();
            builder.Services.AddSingleton<IStaticTokenAuthProvider, StaticTokenAuthProvider>();

            // Infrastructure placeholders
            builder.Services.AddSingleton<IKafkaProducer, KafkaProducerStub>();
            builder.Services.AddSingleton<IKafkaConsumer, KafkaConsumerStub>();
            builder.Services.AddSingleton<IRedisService, RedisServiceStub>();
            builder.Services.AddSingleton<ITcpClientService, TcpClientServiceStub>();

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

            // Load handlers from configuration
            var apiConfigurator = app.Services.GetRequiredService<ApiConfigurator>();
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