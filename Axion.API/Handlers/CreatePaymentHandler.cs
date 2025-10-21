using System.Text.Json;
using Axion.API.Models;
using Axion.API.Services;
using Axion.API.Services.Abstraction;

namespace Axion.API.Handlers;

public class CreatePaymentHandler(IKafkaProducer producer) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        // Simple validation for demo
        var body = request.Body ?? JsonDocument.Parse("{}").RootElement;
        
        // TODO: Add madding to DTO
        
        // Send event to Kafka (placeholder)
        await producer.ProduceAsync("payments-created", body.GetRawText());
        return new ApiResponse
        {
            StatusCode = 200, Data = new
            {
                result = "created"
            }
        };
    }
}