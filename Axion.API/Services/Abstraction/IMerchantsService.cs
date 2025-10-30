namespace Axion.API.Services.Abstraction;

public interface IMerchantsService
{
    Task<List<Dictionary<string, object?>>?> GetMerchantsAsync();
    Task<List<Dictionary<string, object?>>?> CreateMerchantAsync(string name, string email);
}


