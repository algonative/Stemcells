namespace StemCellsPro.Shared.Requests;

public class AddFormRequest
{
    public string UserName { get; set; } = string.Empty;
    public string FormName { get; set; } = string.Empty;
    public Dictionary<string, string> FormData { get; set; } = new();
    public string Attachments { get; set; } = string.Empty;
}

public class GetFormDataRequest
{
    public string Query { get; set; } = string.Empty;
}
