using CustomerSupport.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CustomerSupport.Application.DTOs.Tickets
{
  public sealed class TicketQueryParameters
  {
    // Paging starts from page 1.
    [Range(1, int.MaxValue)]
    public int PageNumber { get; set; } = 1;

    // Limit the maximum page size so one request cannot return an excessive number of records.
    [Range(1, 100)]
    public int PageSize { get; set; } = 10;

    // Free-text search can later match Ticket Number, Subject, or Description.
    [StringLength(200)]
    public string? Search { get; set; }

    // Optional master-data filters.
    [Range(1, int.MaxValue)]
    public int? ProductId { get; set; }

    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    [EnumDataType(typeof(TicketPriorityType))]
    public TicketPriorityType? PriorityId { get; set; }

    [EnumDataType(typeof(TicketStatusType))]
    public TicketStatusType? StatusId { get; set; }

    // Administrative/support filters.
    [Range(1, int.MaxValue)]
    public int? CustomerId { get; set; }

    [Range(1, int.MaxValue)]
    public int? AssignedToUserId { get; set; }

    // createdAt is the default sort field.
    [StringLength(50)]
    public string SortBy { get; set; } = "createdAt";

    // Only ascending or descending sort directions are accepted.
    [RegularExpression(
        "^(?i:asc|desc)$",
        ErrorMessage = "SortDirection must be either asc or desc.")]
    public string SortDirection { get; set; } = "desc";
  }
}
