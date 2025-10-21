using System.Text.Json;
using Axion.API.Auth;
using Axion.API.Config;
using Axion.API.Middleware;
using Axion.API.Registry;
using Axion.API.Services.Abstraction;
using Axion.API.Services.Implementation;

namespace Axion.API;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // Config
        builder.Configuration.AddJsonFile("api_routes.json", optional: false, reloadOnChange: true);
        
        // Services
        builder.Services.AddSingleton<HandlerRegistry>();
        builder.Services.AddSingleton<ApiConfigurator>();
        
        // Auth providers
        builder.Services.AddSingleton<IAuthProvider, JwtAuthProvider>();
        builder.Services.AddSingleton<StaticTokenAuthProvider>();
        
        // Infrastructure placeholders
        builder.Services.AddSingleton<IKafkaProducer, KafkaProducerStub>();
        builder.Services.AddSingleton<IKafkaConsumer, KafkaConsumerStub>();
        builder.Services.AddSingleton<IRedisService, RedisServiceStub>();
        builder.Services.AddSingleton<ITcpClientService, TcpClientServiceStub>();
        
        builder.Services.AddControllers().AddJsonOptions(o => {
            o.JsonSerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
        });
        
        var app = builder.Build();
        
        // Middleware pipeline
        app.UseMiddleware<RequestLoggingMiddleware>();
        app.UseMiddleware<AuthMiddleware>();
        
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
}