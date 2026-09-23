namespace CustomerSupport.Application.Models
{
  public sealed class PagedResult<T>
  {
    // Records belonging to the current page.
    public IReadOnlyCollection<T> Items { get; set; } = [];
    public int PageNumber { get; set; }
    public int PageSize { get; set; }

    // Total number of matching records before Skip/Take is applied.
    public int TotalRecords { get; set; }

    // Total number of pages calculated from TotalRecords and PageSize.
    public int TotalPages { get; set; }

    // Convenience properties that help API clients enable/disable paging controls.
    public bool HasPreviousPage => PageNumber > 1;
    public bool HasNextPage => PageNumber < TotalPages;

    public static PagedResult<T> Create(
        IReadOnlyCollection<T> items,
        int totalRecords,
        int pageNumber,
        int pageSize)
    {
      // Centralize paging calculations so every paged query behaves consistently.
      return new PagedResult<T>
      {
        Items = items,
        PageNumber = pageNumber,
        PageSize = pageSize,
        TotalRecords = totalRecords,

        // Avoid division when there are no matching records.
        TotalPages = totalRecords == 0 ? 0 : (int)Math.Ceiling(totalRecords / (double)pageSize)
      };
    }
  }
}
