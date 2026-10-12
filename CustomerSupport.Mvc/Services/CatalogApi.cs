using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Interfaces.Services;

namespace CustomerSupport.Mvc.Services
{
    public sealed class CatalogApi : ICatalogApi
    {
        private readonly IApiClient _api;

        public CatalogApi(IApiClient api)
        {
            _api = api;
        }

        public Task<ApiResult<List<Product>>> ProductsAsync(bool all = false)
        {
            return _api.GetAsync<List<Product>>(all ? "api/products/all" : "api/products");
        }

        public Task<ApiResult<Product>> ProductAsync(int id)
        {
            return _api.GetAsync<Product>($"api/products/{id}");
        }

        public Task<ApiResult<Product>> SaveProductAsync(int? id,string name,string? description,bool active)
        {
            return _api.SendAsync<Product>(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            id.HasValue ? $"api/products/{id.Value}" : "api/products",
            id.HasValue ? new { name, description, isActive = active } : (object)new { name, description });
        }

        public Task<ApiResult<List<TicketCategory>>> CategoriesAsync(bool all = false)
        {
            return _api.GetAsync<List<TicketCategory>>(all ? "api/ticket-categories/all" : "api/ticket-categories");
        }

        public Task<ApiResult<TicketCategory>> CategoryAsync(int id)
        {
            return _api.GetAsync<TicketCategory>($"api/ticket-categories/{id}");
        }

        public Task<ApiResult<TicketCategory>> SaveCategoryAsync(int? id,string name,string? description,bool active)
        {
            return _api.SendAsync<TicketCategory>(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            id.HasValue ? $"api/ticket-categories/{id.Value}" : "api/ticket-categories",
            id.HasValue ? new { name, description, isActive = active } : (object)new { name, description });
        }

        public Task<ApiResult<List<MasterValue>>> PrioritiesAsync()
        {
            return _api.GetAsync<List<MasterValue>>("api/master-data/ticket-priorities");
        }

        public Task<ApiResult<List<MasterValue>>> StatusesAsync()
        {
            return _api.GetAsync<List<MasterValue>>("api/master-data/ticket-statuses");
        }
    }
}
