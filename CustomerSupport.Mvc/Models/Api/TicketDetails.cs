namespace CustomerSupport.Mvc.Models.Api
{
    public sealed class TicketDetails
    {
        public int Id { get; set; }
        public string TicketNumber { get; set; } = string.Empty;
        public string Subject { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int CustomerId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public int ProductId { get; set; }
        public string ProductName { get; set; } = string.Empty;
        public int? CategoryId { get; set; }
        public string? CategoryName { get; set; }
        public int PriorityId { get; set; }
        public string PriorityName { get; set; } = string.Empty;
        public int StatusId { get; set; }
        public string StatusName { get; set; } = string.Empty;
        public int? AssignedToUserId { get; set; }
        public string? AssignedToUserName { get; set; }
        public DateTime? ResolvedAt { get; set; }
        public DateTime? ClosedAt { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public List<TicketComment> Comments { get; set; } = [];
        public List<TicketHistory> History { get; set; } = [];
        public List<TicketAttachment> Attachments { get; set; } = [];
    }
}
