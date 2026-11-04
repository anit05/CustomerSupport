using System.ComponentModel.DataAnnotations;

namespace CustomerSupport.Application.DTOs.AI
{
    public sealed class GenerateTicketAIRequestDTO
    {
        // Supported values:
        // Summary, Category, Priority, Sentiment,
        // ReplyDraft, InternalNote.
        [Required]
        [StringLength(30)]
        public string Feature { get; set; } = string.Empty;

        // Optional article to use when drafting a reply
        // or preparing internal notes.
        [Range(1, int.MaxValue)]
        public int? KnowledgeBaseArticleId { get; set; }
    }
}
