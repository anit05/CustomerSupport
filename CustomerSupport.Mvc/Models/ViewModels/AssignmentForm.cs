using System.ComponentModel.DataAnnotations;
using CustomerSupport.Mvc.Models.Api;
using Microsoft.AspNetCore.Http;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    public sealed class AssignmentForm
    {
        [Range(1, int.MaxValue)] public int TicketId { get; set; }
        [Range(1, int.MaxValue)] public int SupportExecutiveId { get; set; }
    }
}
