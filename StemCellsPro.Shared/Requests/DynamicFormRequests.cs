namespace StemCellsPro.Shared.Requests;

public class AddFormRequest
{
    // Kept for backward compatibility. Prefer resolving the user from token claims.
    public string UserName { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public Dictionary<string, string> FormData { get; set; } = new();
    public string Attachments { get; set; } = string.Empty;
}

public class FormSearchRequest
{
    public string FormName { get; set; } = string.Empty;
    public int PageNumber { get; set; } = 1;
    public int PageSize { get; set; } = 25;
    public string? SortBy { get; set; }
    public bool SortDescending { get; set; }
    public List<FormFilterRequest> Filters { get; set; } = new();
}

public class FormFilterRequest
{
    public string Field { get; set; } = string.Empty;
    public string Operator { get; set; } = "eq";
    public string? Value { get; set; }
    public List<string>? Values { get; set; }
}
