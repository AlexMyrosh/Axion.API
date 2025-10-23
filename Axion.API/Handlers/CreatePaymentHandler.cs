using System.Text.Json;
using Axion.API.Models;
using Axion.API.Services.Abstraction;

namespace Axion.API.Handlers;

public class CreatePaymentHandler(IKafkaProducer producer) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        // Simple validation for demo
        var body = request.Body ?? JsonDocument.Parse("{}").RootElement;
        
        // Send event to Kafka (placeholder)
        await producer.ProduceAsync("payments-created", body.GetRawText());
        return ApiResponse.Success(new { message = "Success operation" });
    }
}