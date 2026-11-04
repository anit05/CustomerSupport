using CustomerSupport.Application.DTOs.AI;
namespace CustomerSupport.Application.Interfaces.Services
{
    public interface IPromptExperimentService
    {
        Task<PromptExperimentResponseDTO> TestAsync(
            PromptExperimentRequestDTO request,
            CancellationToken cancellationToken = default);
    }
}