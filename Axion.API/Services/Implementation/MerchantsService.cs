using Axion.API.DbRepositories.Abstraction;
using Axion.API.Services.Abstraction;

namespace Axion.API.Services.Implementation;

public class MerchantsService(IPostgresRepository postgresRepository) : IMerchantsService
{
    public Task<List<Dictionary<string, object?>>?> GetMerchantsAsync()
    {
        return postgresRepository.DbExecuteAsync("merchants", "GetMerchants");
    }

    public Task<List<Dictionary<string, object?>>?> CreateMerchantAsync(string name, string email)
    {
        var parameters = new Dictionary<string, object>
        {
            ["name"] = name,
            ["email"] = email
        };
        return postgresRepository.DbExecuteAsync("merchants", "CreateMerchant", parameters);
    }
}


