namespace CustomerSupport.Mvc.Models.Api
{
    public sealed class ContentSearch
    {
        public string? Search { get; set; }
        public int? ProductId { get; set; }
        public int PageNumber { get; set; } = 1;
        public int PageSize { get; set; } = 10;
    }
}
