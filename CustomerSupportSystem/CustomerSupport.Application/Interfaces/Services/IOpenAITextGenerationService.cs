using CustomerSupport.Application.DTOs.AI;
namespace CustomerSupport.Application.Interfaces.Services
{
    // Defines the AI operations our application can request.
    public interface IOpenAITextGenerationService
    {
        // Existing operation: returns ordinary generated text.
        Task<AITextResultDTO> GenerateAsync(
            string developerInstructions,
            string userInput,
            CancellationToken cancellationToken = default);

        // New operation: asks for JSON that follows a schema.
        // The result still contains text until we deserialize it.
        Task<AITextResultDTO> GenerateStructuredAsync(
            string developerInstructions,
            string userInput,
            string jsonSchema,
            CancellationToken cancellationToken = default);
    }
}