using CustomerSupport.Application.DTOs.FAQs;
using CustomerSupport.Application.Models;

namespace CustomerSupport.Application.Interfaces.Services
{
  // Defines FAQ use cases.
  public interface IFAQService
  {
    Task<PagedResult<FAQResponseDTO>> SearchAsync(FAQQueryParameters parameters);

    Task<FAQResponseDTO> GetByIdAsync(int id);

    Task<FAQResponseDTO> CreateAsync(CreateFAQRequestDTO request);

    Task<FAQResponseDTO> UpdateAsync(int id, UpdateFAQRequestDTO request);

    // Soft delete: sets IsActive to false.
    Task DeactivateAsync(int id);
  }
}
