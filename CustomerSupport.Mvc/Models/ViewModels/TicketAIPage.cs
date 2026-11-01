
using CustomerSupport.Mvc.Models.Api;

namespace CustomerSupport.Mvc.Models.ViewModels
{
    // Contains all information displayed on the Ticket AI Assistant page.
    public sealed class TicketAIPage
    {
        // Existing ticket information.
        public TicketDetails Ticket { get; set; } = new();

        // Structured analysis returned by AI.
        public TicketAIAnalysisResult? Analysis
        {
            get; set;
        }

        // Generated customer reply or internal note.
        public TicketAIDraftResult? Draft
        {
            get; set;
        }

        // Heading displayed above the generated draft.
        public string DraftTitle { get; set; } = string.Empty;

        // Optional Knowledge Base article selected by the user.
        public int? KnowledgeBaseArticleId
        {
            get; set;
        }

        // Available Knowledge Base articles.
        public List<KnowledgeArticle> Articles { get; set; } = [];

        // Controls whether the reviewed comment is internal.
        public bool IsInternal { get; set; } = true;

        // Confirmation before saving customer-visible comments.
        public bool ConfirmCustomerVisible
        {
            get; set;
        }
    }
}
