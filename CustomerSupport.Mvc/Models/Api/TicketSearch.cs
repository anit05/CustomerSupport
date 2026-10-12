namespace CustomerSupport.Mvc.Models.Api
{
    public sealed class TicketSearch
    {
        public string? Search { get; set; }
        public int? StatusId { get; set; }
        public int? PriorityId { get; set; }
        public int? ProductId { get; set; }
        public int? CategoryId { get; set; }
        public int? CustomerId { get; set; }
        public int? AssignedToUserId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
        public string SortBy { get; set; } = "createdAt";
        public string SortDirection { get; set; } = "desc";
    }
}
