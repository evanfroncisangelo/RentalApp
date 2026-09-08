namespace RentalApp.Pages.Shared;

public class PaginationModel
{
    public string PageUrl { get; set; } = string.Empty;
    public int PageNumber { get; set; }
    public int TotalPages { get; set; }
    public string PageParameterName { get; set; } = "PageNumber";
    public Dictionary<string, string> RouteValues { get; set; } = [];

    public bool HasPrevious => PageNumber > 1;
    public bool HasNext => PageNumber < TotalPages;
}
