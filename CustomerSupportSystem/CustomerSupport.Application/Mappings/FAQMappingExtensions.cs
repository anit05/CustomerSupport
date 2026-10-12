using CustomerSupport.Application.DTOs.FAQs;
using CustomerSupport.Domain.Entities;

namespace CustomerSupport.Application.Mappings
{
  // Contains reusable mappings for FAQ responses.
  public static class FAQMappingExtensions
  {
    public static FAQResponseDTO ToResponseDTO(this FAQ faq)
    {
      return new FAQResponseDTO
      {
        Id = faq.Id,
        Question = faq.Question,
        Answer = faq.Answer,
        ProductId = faq.ProductId,
        ProductName = faq.Product?.Name,
        IsActive = faq.IsActive,
        CreatedAt = faq.CreatedAt,
        UpdatedAt = faq.UpdatedAt
      };
    }

    // Convert CreateFAQRequestDTO to FAQ Entity.
    public static FAQ ToEntity(this CreateFAQRequestDTO request, int createdBy)
    {
      return new FAQ
      {
        Question = request.Question.Trim(),
        Answer = request.Answer.Trim(),
        ProductId = request.ProductId,

        // Every newly created FAQ starts as active.
        IsActive = true,

        // Record which Administrator created the FAQ.
        CreatedBy = createdBy
      };
    }

    // Convert UpdateFAQRequestDTO to FAQ Entity.
    public static void MapFrom(this FAQ faq, UpdateFAQRequestDTO request, int updatedBy)
    {
      // Update the editable FAQ properties.
      faq.Question = request.Question.Trim();
      faq.Answer = request.Answer.Trim();
      faq.ProductId = request.ProductId;
      faq.IsActive = request.IsActive;

      // Maintain audit information for the update.
      faq.UpdatedAt = DateTime.UtcNow;
      faq.UpdatedBy = updatedBy;
    }
  }
}

