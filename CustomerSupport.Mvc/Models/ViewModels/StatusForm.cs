using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class StatusForm
    {
        [Range(1, int.MaxValue)] public int TicketId { get; set; }
        [Range(1, 5)] public int StatusId { get; set; }
        public string? Remarks { get; set; }
    }
}
