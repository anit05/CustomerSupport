
using System.ComponentModel.DataAnnotations;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    // Accepts an optional Knowledge Base article for draft generation.
    public sealed class TicketAIDraftForm
    {
        // The backend checks whether this article can be used.
        [Range(1, int.MaxValue)]
        public int? KnowledgeBaseArticleId
        {
            get; set;
        }
    }
}
