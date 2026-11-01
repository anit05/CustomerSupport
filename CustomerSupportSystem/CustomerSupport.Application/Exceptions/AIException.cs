namespace CustomerSupport.Application.Exceptions
{
  // Common exception for AI-related operations.
  // Can be used for ticket summarization, reply generation,
  // embeddings, RAG, and other upcoming AI features.
  public sealed class AIException : Exception
  {
    public AIException(string message) : base(message)
    {
    }
  }
}
