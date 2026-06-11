using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Shared.Requests;
using StemCellsPro.Shared.Responses;
using System.Data;

namespace StemCellsPro.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Handled by CustomAuthMiddleware
public class DataController : ControllerBase
{
    private readonly IFormDataRepository _formDataRepository;

    public DataController(IFormDataRepository formDataRepository)
    {
        _formDataRepository = formDataRepository;
    }

    [HttpPost("add-form")]
    public async Task<IActionResult> AddData([FromBody] AddFormRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FormName))
            return BadRequest(new ApiResponse<object>(null, "Form Name is missing.") { Success = false });

        if (string.IsNullOrWhiteSpace(request.UserName))
            return BadRequest(new ApiResponse<object>(null, "User Name is missing.") { Success = false });

        var result = await _formDataRepository.AddFormAsync(request.FormName, request.UserName, request.FormData, request.Attachments);

        return Ok(new ApiResponse<int>(result, "Form data saved successfully."));
    }

    [HttpDelete("delete-form/{id}")]
    public async Task<IActionResult> DeleteData(int id)
    {
        var result = await _formDataRepository.DeleteFormAsync(id);

        if (result == 0)
            return NotFound(new ApiResponse<object>(null, "Form data not found.") { Success = false });

        return Ok(new ApiResponse<int>(result, $"Form data with Id {id} deleted successfully."));
    }

    [HttpPost("get-data")]
    public async Task<IActionResult> GetData([FromBody] GetFormDataRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Query))
            return BadRequest(new ApiResponse<object>(null, "Query is missing.") { Success = false });

        try
        {
            var dataTable = await _formDataRepository.GetFormDataAsync(request.Query);
            
            // Convert DataTable to List of Dictionaries for System.Text.Json compatibility
            var resultList = new List<Dictionary<string, object>>();
            foreach (DataRow row in dataTable.Rows)
            {
                var dict = new Dictionary<string, object>();
                foreach (DataColumn col in dataTable.Columns)
                {
                    dict[col.ColumnName] = row[col] == DBNull.Value ? null : row[col];
                }
                resultList.Add(dict);
            }

            return Ok(new ApiResponse<List<Dictionary<string, object>>>(resultList, "Data fetched successfully."));
        }
        catch (Exception ex)
        {
            return BadRequest(new ApiResponse<object>(null, $"Error executing query: {ex.Message}") { Success = false });
        }
    }
}
