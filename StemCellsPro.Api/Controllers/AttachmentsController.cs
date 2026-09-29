using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Domain.Models;

namespace StemCellsPro.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AttachmentsController : ControllerBase
    {
        private readonly IAttachmentsRepository _repository;
        private readonly IMayanEdmsService _mayanService;

        public AttachmentsController(IAttachmentsRepository repository, IMayanEdmsService mayanService)
        {
            _repository = repository;
            _mayanService = mayanService;
        }

        [HttpPost("upload")]
        public async Task<IActionResult> UploadAttachment(IFormFile file, [FromForm] string formName, [FromForm] int formId, [FromForm] string? userName)
        {
            if (file == null || file.Length == 0) return BadRequest(new { success = false, message = "File is missing." });
            if (formId <= 0) return BadRequest(new { success = false, message = "Invalid FormId." });

            try
            {
                
                
                using var ms = new MemoryStream();
                await file.CopyToAsync(ms);
                var fileBytes = ms.ToArray();

                var mayanDocumentId = await _mayanService.UploadDocumentAsync(file.FileName, fileBytes, file.ContentType);

                var attachment = new FormAttachment
                {
                    AttachmentId = Guid.NewGuid(),
                    FormName = formName,
                    FormId = formId,
                    MayanDocumentId = mayanDocumentId,
                    FileName = file.FileName,
                    ContentType = file.ContentType,
                    FileSize = file.Length,
                    UploadedBy = userName ?? "System",
                    UploadedAt = DateTime.UtcNow,
                    Status = "Active",
                    CreatedAt = DateTime.UtcNow,
                    UpdatedAt = DateTime.UtcNow
                };

                await _repository.AddAttachmentAsync(attachment);

                return Ok(new { success = true, attachmentId = attachment.AttachmentId, fileName = attachment.FileName });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        [HttpGet("{formName}/{formId}")]
        public async Task<IActionResult> GetAttachments(string formName, int formId)
        {
            var attachments = await _repository.GetAttachmentsAsync(formName, formId);
            var safeResponse = attachments.Select(a => new 
            {
                attachmentId = a.AttachmentId,
                fileName = a.FileName,
                contentType = a.ContentType,
                fileSize = a.FileSize,
                uploadedAt = a.UploadedAt,
                status = a.Status
            });
            return Ok(new { success = true, data = safeResponse });
        }

        [HttpGet("{attachmentId}/download")]
        public async Task<IActionResult> DownloadAttachment(Guid attachmentId)
        {
            var attachment = await _repository.GetAttachmentByIdAsync(attachmentId);
            if (attachment == null) return NotFound();

            var (fileBytes, fileName, mimeType) = await _mayanService.DownloadDocumentAsync(attachment.MayanDocumentId);
            return File(fileBytes, mimeType, fileName);
        }

        [HttpDelete("{attachmentId}")]
        public async Task<IActionResult> DeleteAttachment(Guid attachmentId)
        {
            var attachment = await _repository.GetAttachmentByIdAsync(attachmentId);
            if (attachment == null) return NotFound();

            try
            {
                await _mayanService.DeleteDocumentAsync(attachment.MayanDocumentId);
            }
            catch
            {
                // Continue DB deletion even if Mayan deletion fails/was already deleted
            }
            
            await _repository.DeleteAttachmentAsync(attachmentId);
            return Ok(new { success = true });
        }
    }
}






