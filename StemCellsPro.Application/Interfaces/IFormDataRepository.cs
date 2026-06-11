using System.Data;

namespace StemCellsPro.Application.Interfaces;

public interface IFormDataRepository
{
    Task<int> AddFormAsync(string formName, string userName, Dictionary<string, string> formData, string attachments = "");
    Task<int> DeleteFormAsync(int id);
    Task<DataTable> GetFormDataAsync(string query);
}
