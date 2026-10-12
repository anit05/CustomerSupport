using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class ContentForm
    {
        public int Id { get; set; }
        [Required] public string Title { get; set; } = string.Empty;
        [Required] public string Content { get; set; } = string.Empty;
        public int? ProductId { get; set; }
        public bool IsActive { get; set; } = true;
        public bool IsPublished { get; set; }
        public List<Product> Products { get; set; } = [];
    }
}
