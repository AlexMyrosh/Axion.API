using System.Text.Json;
using Axion.API.Models;
using Axion.API.Services.Abstraction;

namespace Axion.API.Handlers;

public class CreatePaymentHandler(IKafkaProducer producer) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        // Demo logic
        await producer.ProduceAsync("payments-created", "{}");
        return ApiResponse.Success(new { message = "Success operation" });
    }
}