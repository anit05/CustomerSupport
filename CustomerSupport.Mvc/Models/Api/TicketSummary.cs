namespace CustomerSupport.Mvc.Models.Api
{
    public sealed class TicketSummary
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string CustomerName { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public string? CategoryName { get; set; }
        public int PriorityId { get; set; }
        public string PriorityName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public string? AssignedToUserName { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }
}
