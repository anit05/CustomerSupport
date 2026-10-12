using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class UploadForm
    {
        public int TicketId { get; set; }
        [Required] public IFormFile? File { get; set; }
    }
}
