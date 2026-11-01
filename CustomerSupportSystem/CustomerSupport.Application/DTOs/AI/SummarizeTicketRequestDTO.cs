using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.AI
{
  public sealed class SummarizeTicketRequestDTO
  {
    // Ticket comments that will be sent to the AI model.
    // Required prevents missing or empty input.
    // StringLength limits the amount of text accepted.
    [Required(ErrorMessage = "Ticket comments are required.")]
    [StringLength(12000, MinimumLength = 1, ErrorMessage = "Ticket comments must not exceed 12000 characters.")]
    public string TicketComments { get; set; } = string.Empty;
  }
}
