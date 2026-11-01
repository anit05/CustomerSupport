using CustomerSupport.Mvc.Models.Api;
using CustomerSupport.Mvc.Interfaces.Services;

namespace CustomerSupport.Mvc.Services
{
    public sealed class ContentApi : IContentApi
    {
        private readonly IApiClient _api;

        public ContentApi(IApiClient api)
        {
            _api = api;
        }

        private static string Query(ContentSearch s)
        {
            return "?" + string.Join("&", new Dictionary<string, string?>
            {
                ["search"] = s.Search,
                ["productId"] = s.ProductId?.ToString(),
                ["pageNumber"] = Math.Max(1, s.PageNumber).ToString(),
                ["pageSize"] = Math.Clamp(s.PageSize, 1, 100).ToString()
            }
            .Where(x => !string.IsNullOrWhiteSpace(x.Value))
            .Select(x => $"{Uri.EscapeDataString(x.Key)}={Uri.EscapeDataString(x.Value!)}"));
        }

        public Task<ApiResult<PagedResult<KnowledgeArticle>>> ArticlesAsync(ContentSearch s)
        {
            return _api.GetAsync<PagedResult<KnowledgeArticle>>("api/knowledge-base" + Query(s));
        }

        public Task<ApiResult<KnowledgeArticle>> ArticleAsync(int id)
        {
            return _api.GetAsync<KnowledgeArticle>($"api/knowledge-base/{id}");
        }

        public Task<ApiResult<KnowledgeArticle>> SaveArticleAsync(int? id,string title,string content,int? productId,bool published,bool active)
        {
            return _api.SendAsync<KnowledgeArticle>(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            id.HasValue ? $"api/knowledge-base/{id.Value}" : "api/knowledge-base",
            id.HasValue ? new { title, content, productId, isPublished = published, isActive = active }
            : (object)new { title, content, productId, isPublished = published });
        }

        public Task<ApiResult<object>> DeactivateArticleAsync(int id)
        {
            return _api.SendAsync<object>(HttpMethod.Delete,$"api/knowledge-base/{id}");
        }

        public Task<ApiResult<PagedResult<Faq>>> FaqsAsync(ContentSearch s)
        {
            return _api.GetAsync<PagedResult<Faq>>("api/faqs" + Query(s));
        }

        public Task<ApiResult<Faq>> FaqAsync(int id)
        {
            return _api.GetAsync<Faq>($"api/faqs/{id}");
        }

        public Task<ApiResult<Faq>> SaveFaqAsync(int? id,string question,string answer,int? productId,bool active)
        {
            return _api.SendAsync<Faq>(id.HasValue ? HttpMethod.Put : HttpMethod.Post,
            id.HasValue ? $"api/faqs/{id.Value}" : "api/faqs",
            id.HasValue ? new { question, answer, productId, isActive = active }
            : (object)new { question, answer, productId });
        }

        public Task<ApiResult<object>> DeactivateFaqAsync(int id)
        {
            return _api.SendAsync<object>(HttpMethod.Delete,$"api/faqs/{id}");
        }
    }
}
