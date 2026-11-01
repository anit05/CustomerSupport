using CustomerSupport.Mvc.Models.Api;

namespace CustomerSupport.Mvc.Interfaces.Services
{
    public interface IContentApi
    {
        Task<ApiResult<PagedResult<KnowledgeArticle>>> ArticlesAsync(ContentSearch search);

        Task<ApiResult<KnowledgeArticle>> ArticleAsync(int id);

        Task<ApiResult<KnowledgeArticle>> SaveArticleAsync(int? id, string title, string content,int? productId,bool published,bool active);

        Task<ApiResult<object>> DeactivateArticleAsync(int id);

        Task<ApiResult<PagedResult<Faq>>> FaqsAsync(ContentSearch search);

        Task<ApiResult<Faq>> FaqAsync(int id);

        Task<ApiResult<Faq>> SaveFaqAsync(int? id,string question,string answer,int? productId,bool active);

        Task<ApiResult<object>> DeactivateFaqAsync(int id);
    }
}
