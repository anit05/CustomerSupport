using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class TicketCreateForm
    {
        [Range(1, int.MaxValue)] public int ProductId { get; set; }
        public int? CategoryId { get; set; }
        [Range(1, 4)] public int PriorityId { get; set; } = 2;
        [Required, StringLength(250, MinimumLength = 5)] public string Subject { get; set; } = string.Empty;
        [Required, StringLength(5000, MinimumLength = 10)] public string Description { get; set; } = string.Empty;
        public List<Product> Products { get; set; } = [];
        public List<TicketCategory> Categories { get; set; } = [];
        public List<MasterValue> Priorities { get; set; } = [];
    }
}
