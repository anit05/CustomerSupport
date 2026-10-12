namespace CustomerSupport.Mvc.Models.Api
{
    public sealed class TicketCreateRequest
    {
        public int ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int PriorityId { get; set; } = 2;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
    }
}
