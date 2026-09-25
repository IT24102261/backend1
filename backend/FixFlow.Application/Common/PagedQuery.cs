namespace FixFlow.Application.Common;

public class PagedQuery
{
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 20;
    public string? Search { get; set; }
    public string? SortBy { get; set; }
    public string? SortDir { get; set; }
    public string? Status { get; set; }
    public string? Role { get; set; }

    public int Skip => Math.Max(Page - 1, 0) * Take;
    public int Take => Math.Clamp(PageSize, 1, 100);
    public bool Descending => string.Equals(SortDir, "desc", StringComparison.OrdinalIgnoreCase);
}
