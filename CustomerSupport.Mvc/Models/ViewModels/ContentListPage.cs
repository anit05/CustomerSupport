using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class ContentListPage<T>
    {
        public ContentSearch Search { get; set; } = new();
        public PagedResult<T> Results { get; set; } = new();
        public List<Product> Products { get; set; } = [];
        public bool Management { get; set; }
    }
}
