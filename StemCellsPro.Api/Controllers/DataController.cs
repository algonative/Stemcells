using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Shared.Requests;
using StemCellsPro.Shared.Responses;
using System.Security.Claims;

namespace StemCellsPro.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize] // Requires a valid ApiToken bearer token.
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
            return BadRequest(new ApiResponse<object>("Form Name is missing."));

        var userName = GetCurrentUserName(request.UserName);
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized(new ApiResponse<object>("Unable to resolve the current user."));

        var result = await _formDataRepository.AddFormAsync(request.FormName, userName, request.FormData, request.Attachments);

        return Ok(result);
    }

    [HttpPut("update-form/{formName}/{id:int}")]
    public async Task<IActionResult> UpdateData(string formName, int id, [FromBody] AddFormRequest request)
    {
        if (string.IsNullOrWhiteSpace(formName))
            return BadRequest(new ApiResponse<object>("Form Name is missing."));

        var userName = GetCurrentUserName(request.UserName);
        if (string.IsNullOrWhiteSpace(userName))
            return Unauthorized(new ApiResponse<object>("Unable to resolve the current user."));

        var result = await _formDataRepository.UpdateFormAsync(formName, id, userName, request.FormData, request.Attachments);

        return Ok(result);
    }

    [HttpDelete("delete-form/{formName}/{id:int}")]
    public async Task<IActionResult> DeleteData(string formName, int id, [FromQuery] string userName = "")
    {
        if (string.IsNullOrWhiteSpace(formName))
            return BadRequest(new ApiResponse<object>("Form Name is missing."));

        var currentUser = GetCurrentUserName(userName);
        if (string.IsNullOrWhiteSpace(currentUser))
            return Unauthorized(new ApiResponse<object>("Unable to resolve the current user."));

        var result = await _formDataRepository.DeleteFormAsync(formName, id, currentUser);

        if (result == 0)
            return NotFound(new ApiResponse<object>("Form data not found."));

        return Ok(new ApiResponse<int>(result, $"Form data with Id {id} deleted successfully."));
    }

    [HttpDelete("delete-form/{id:int}")]
    public IActionResult DeleteDataWithoutFormName(int id)
    {
        return BadRequest(new ApiResponse<object>(
            "Form Name is required. Use DELETE /api/Data/delete-form/{formName}/{id}.")
        );
    }

    [HttpPost("search")]
    [HttpPost("get-data")]
    public async Task<IActionResult> GetData([FromBody] FormSearchRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.FormName))
            return BadRequest(new ApiResponse<object>("Form Name is missing."));

        var result = await _formDataRepository.SearchFormDataAsync(request);

        return Ok(result);
    }

    private string GetCurrentUserName(string fallbackUserName)
    {
        var candidates = new[]
        {
            User.FindFirst(ClaimTypes.Name)?.Value,
            User.FindFirst("UserName")?.Value,
            User.Identity?.Name,
            fallbackUserName
        };

        return candidates.FirstOrDefault(value => !string.IsNullOrWhiteSpace(value)) ?? string.Empty;
    }
}
