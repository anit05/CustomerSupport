using CustomerSupport.Mvc.Models.Api;

namespace CustomerSupport.Mvc.Interfaces.Services
{
    public interface ICatalogApi
    {
        Task<ApiResult<List<Product>>> ProductsAsync(bool all = false);

        Task<ApiResult<Product>> ProductAsync(int id);

        Task<ApiResult<Product>> SaveProductAsync(int? id, string name, string? description, bool active);

        Task<ApiResult<List<TicketCategory>>> CategoriesAsync(bool all = false);

        Task<ApiResult<TicketCategory>> CategoryAsync(int id);

        Task<ApiResult<TicketCategory>> SaveCategoryAsync(int? id, string name, string? description, bool active);

        Task<ApiResult<List<MasterValue>>> PrioritiesAsync();

        Task<ApiResult<List<MasterValue>>> StatusesAsync();
    }
}
