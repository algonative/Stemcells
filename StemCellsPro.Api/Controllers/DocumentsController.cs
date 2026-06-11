using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Shared.Responses;

namespace StemCellsPro.Api.Controllers;

[Route("api/[controller]")]
[ApiController]
[Authorize]
public class DocumentsController : ControllerBase
{
    private readonly IMayanEdmsService _mayanEdmsService;

    public DocumentsController(IMayanEdmsService mayanEdmsService)
    {
        _mayanEdmsService = mayanEdmsService;
    }

    [HttpPost("upload")]
    public async Task<IActionResult> UploadDocument(IFormFile file, [FromForm] int documentTypeId = 1)
    {
        if (file == null || file.Length == 0)
        {
            return BadRequest(new ApiResponse<object>(null, "No file uploaded.") { Success = false });
        }

        using var memoryStream = new MemoryStream();
        await file.CopyToAsync(memoryStream);
        var fileBytes = memoryStream.ToArray();

        try
        {
            var documentId = await _mayanEdmsService.UploadDocumentAsync(
                file.FileName, 
                fileBytes, 
                file.ContentType, 
                documentTypeId
            );

            return Ok(new ApiResponse<string>(documentId, "Document uploaded to Mayan EDMS successfully."));
        }
        catch (Exception ex)
        {
            return StatusCode(500, new ApiResponse<object>(null, ex.Message) { Success = false });
        }
    }

    [HttpGet("{id}/download")]
    public async Task<IActionResult> DownloadDocument(string id)
    {
        try
        {
            var document = await _mayanEdmsService.DownloadDocumentAsync(id);
            return File(document.FileBytes, document.MimeType, document.FileName);
        }
        catch (Exception ex)
        {
            return NotFound(new ApiResponse<object>(null, ex.Message) { Success = false });
        }
    }
}
