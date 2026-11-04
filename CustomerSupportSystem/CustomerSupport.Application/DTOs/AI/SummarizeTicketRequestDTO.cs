using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.AI
{
    public sealed class SummarizeTicketRequestDTO
    {
        // Ticket comments that the AI model will summarize.
        [Required(ErrorMessage = "Ticket comments are required.")]
        [StringLength(12000, ErrorMessage = "Ticket comments cannot exceed 12000 characters.")]
        public string TicketComments { get; set; } = string.Empty;

        // Optional additional context supplied by the client.
        // Example: Ticket subject, status, priority, or other relevant background information.
        [StringLength(4000, ErrorMessage = "Context information cannot exceed 4000 characters.")]
        public string? ContextInformation { get; set; }

        // Selects the prompting technique for the practical exercise.
        // Supported values:
        // ZeroShot
        // TwoPoint
        // InternalHandover
        // NoResolutionDate
        // FewShot
        // ZeroShot is used when the client does not specify a style.
        [Required]
        [StringLength(30)]
        public string PromptStyle { get; set; } = "ZeroShot";
    }
}