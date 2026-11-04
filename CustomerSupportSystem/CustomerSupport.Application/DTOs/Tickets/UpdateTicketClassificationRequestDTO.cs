using CustomerSupport.Domain.Enums;
using System.ComponentModel.DataAnnotations;

namespace CustomerSupport.Application.DTOs.Tickets
{
  // Contains the approved Category and Priority for a ticket.
  public sealed class UpdateTicketClassificationRequestDTO
  {
    // Null means the ticket has no selected category.
    [Range(1, int.MaxValue)]
    public int? CategoryId { get; set; }

    // The selected priority must be a valid enum value.
    [EnumDataType(typeof(TicketPriorityType))]
    public TicketPriorityType PriorityId { get; set; }
  }
}
