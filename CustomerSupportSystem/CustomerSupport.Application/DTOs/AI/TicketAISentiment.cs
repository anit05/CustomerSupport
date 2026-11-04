namespace CustomerSupport.Application.DTOs.AI
{
    // Possible sentiment values returned by our AI analysis.
    // An enum helps C# work with a known set of labels.
    public enum TicketAISentiment
    {
        Positive, // Customer expresses satisfaction.
        Neutral,  // No strong positive or negative emotion.
        Negative, // Customer expresses dissatisfaction.
        Mixed,    // Both positive and negative language occurs.
        Unclear   // The ticket does not establish sentiment.
    }
}