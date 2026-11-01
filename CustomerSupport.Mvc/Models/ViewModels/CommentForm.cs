using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class CommentForm
    {
        [Required] public int TicketId { get; set; }
        [Required, MinLength(1), StringLength(4000)] public string Content { get; set; } = string.Empty;
        public bool IsInternal { get; set; }
    }
}
