using StemCellsPro.Shared.Responses;
using StemCellsPro.Shared.Requests;

namespace StemCellsPro.Application.Interfaces;

public interface IFormDataRepository
{
    Task<ApiResponse<int>> AddFormAsync(string formName, string userName, Dictionary<string, string> formData, string attachments = "");
    Task<ApiResponse<int>> UpdateFormAsync(string formName, int id, string userName, Dictionary<string, string> formData, string attachments = "");
    Task<int> DeleteFormAsync(string formName, int id, string userName);
    Task<PagedResponse<IReadOnlyList<Dictionary<string, object?>>>> SearchFormDataAsync(FormSearchRequest request);
    Task<IReadOnlyList<Dictionary<string, object?>>> GetAuditTrailAsync(string formName, int id);
}
