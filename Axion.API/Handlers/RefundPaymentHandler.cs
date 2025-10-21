using Axion.API.Models;
using Axion.API.Services;
using Axion.API.Services.Abstraction;

namespace Axion.API.Handlers;

public class RefundPaymentHandler(IKafkaProducer producer) : IApiHandler
{
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        // Demo logic
        await producer.ProduceAsync("payments-refund", "{}");
        return new ApiResponse
        {
            StatusCode = 200, Data = new
            {
                result = "refunded"
            }
        };
    }
}