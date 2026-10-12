namespace CustomerSupport.Application.DTOs.FAQs
{
  public sealed class FAQResponseDTO
  {
    public int Id { get; set; }
    public string Question { get; set; } = string.Empty;
    public string Answer { get; set; } = string.Empty;
    public int? ProductId { get; set; }
    public string? ProductName { get; set; }
    public bool IsActive { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
  }
}
