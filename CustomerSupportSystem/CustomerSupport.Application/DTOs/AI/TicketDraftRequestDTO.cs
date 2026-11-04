using System.ComponentModel.DataAnnotations;
namespace CustomerSupport.Application.DTOs.AI
{
  // Optional approved article to help prepare a reply or note.
  public sealed class TicketDraftRequestDTO
  {
    [Range(1, int.MaxValue)]
    public int? KnowledgeBaseArticleId { get; set; }
  }
}
