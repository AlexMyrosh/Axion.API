using Axion.API.Handlers.Abstraction;
using Axion.API.HttpClient.Abstraction;
using Axion.API.Models;
using Axion.API.Utilities;

namespace Axion.API.Handlers.Implementation;

public class CheckMerchantHandler(IHttpClientWrapper httpClient, JwtTokenGenerator jwtTokenGenerator, ILogger<CheckMerchantHandler> logger) : IApiHandler
{
    private const string Url = "https://ecomm.dev.ukrgasbank.com/api/v1/is_merchant";
    
    public async Task<ApiResponse> HandleAsync(ApiRequest request)
    {
        try
        {
            var merchantName = RequestDataExtractor.GetValue("merchant_name", request.Parsed);
            var payload = new
            {
                merchant_name = merchantName,
                timestamp = TimestampGenerator.GenerateString()
            };
            
            var jwtToken = jwtTokenGenerator.Generate(payload);
            var body = new { merchant_name = merchantName };
            var headers = new Dictionary<string, string>
            {
                ["x-auth-token"] = jwtToken,
                ["content-type"] = "application/json"
            };
            
            var response = await httpClient.SendRequestAsync(HttpMethod.Post, Url, body, headers);
            
            return response.ResponseParsedJson.HasValue 
                ? ApiResponse.Success(response.ResponseParsedJson) 
                : ApiResponse.Success(response.ResponseRaw);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error checking merchant");
            return ApiResponse.Error("500", "Error checking merchant");
        }
    }
}