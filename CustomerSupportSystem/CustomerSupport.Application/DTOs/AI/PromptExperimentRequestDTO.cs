using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.AI
{
    // This DTO is accepted only by our development-time lab.
    public sealed class PromptExperimentRequestDTO
    {
        [Required, StringLength(5000, MinimumLength = 5)]
        public string DeveloperInstructions { get; set; } = "";

        [Required, StringLength(12000, MinimumLength = 1)]
        public string TicketComments { get; set; } = "";

        // Optional practice data supplied by Postman.
        [StringLength(3000)]
        public string? ReferenceText { get; set; }
    }
}