using CustomerSupport.Application.DTOs.Tickets;
namespace CustomerSupport.Application.Services
{
  // Prepares ticket information in a text format that the AI model can understand.
  internal static class TicketAIContextBuilder
  {
    // Builds ticket context with or without internal support comments.
    public static string BuildTicketText(
        TicketDetailsResponseDTO ticket,
        bool includeInternalComments = true)
    {
      // Add the basic ticket details to the AI context.
      var lines = new List<string>
            {
                $"Ticket number: {ticket.TicketNumber}",
                $"Subject: {ticket.Subject}",
                $"Description: {ticket.Description}",
                $"Product: {ticket.ProductName}",
                $"Current status: {ticket.StatusName}"
            };

      // Filter internal comments based on includeInternalComments.
      // Select the latest 15 comments and arrange them chronologically.
      var comments = ticket.Comments
          .Where(x => includeInternalComments || !x.IsInternal)
          .OrderByDescending(x => x.CreatedAt)
          .Take(15)
          .OrderBy(x => x.CreatedAt);

      // Add each selected comment to the AI context.
      foreach (var comment in comments)
      {
        // Read the comment text, using an empty string if null.
        string content = comment.Content ?? string.Empty;

        // Limit each comment to reduce the amount of AI input.
        if (content.Length > 500)
        {
          content = content[..500] + " [excerpt]";
        }

        // Identify whether the comment is internal or public.
        string label = comment.IsInternal
            ? "Support internal comment"
            : "Ticket comment";

        // Add the comment with its visibility label.
        lines.Add($"{label}: {content}");
      }

      // Combine ticket details and comments into one text block.
      string text = string.Join("\n", lines);

      // Limit the overall text to 11000 characters.
      // This prevents excessively large input being sent to AI.
      return text.Length <= 11000
          ? text
          : text[..10950] + "\n[More text omitted]";
    }

    // Prepares ticket information for structured AI analysis.
    public static string ForAnalysis(
        TicketDetailsResponseDTO ticket,
        IEnumerable<string> categories,
        IEnumerable<string> priorities)
    {
      // Include both public and internal comments for analysis.
      string ticketText = BuildTicketText(
          ticket,
          includeInternalComments: true);

      // Send ticket information along with allowed categories
      // and priorities so AI can suggest valid names.
      //
      // XML-like tags separate different parts of the input.
      return "<ticket>\n" + ticketText +
             "\n</ticket>\n" +
             "<allowed_categories>\n" +
             string.Join("\n", categories) +
             "\n</allowed_categories>\n" +
             "<allowed_priorities>\n" +
             string.Join("\n", priorities) +
             "\n</allowed_priorities>";
    }

    // Prepares ticket information for customer replies or internal notes.
    public static string ForDraft(
        TicketDetailsResponseDTO ticket,
        string? articleText,
        bool includeInternalComments)
    {
      // Customer reply: exclude internal comments.
      // Internal note: include both public and internal comments.
      string ticketText = BuildTicketText(
          ticket,
          includeInternalComments);

      // Enclose ticket information within <ticket> tags.
      string input = "<ticket>\n" + ticketText +
                     "\n</ticket>";

      // Add Knowledge Base content only when it is available.
      // This gives AI additional reference information.
      if (!string.IsNullOrWhiteSpace(articleText))
      {
        input += "\n<knowledge_base>\n" +
                 articleText +
                 "\n</knowledge_base>";
      }

      // Return the prepared input for the AI model.
      return input;
    }
  }
}

