using System.Collections.Generic;
using System.Threading.Tasks;
using Dapper;
using StemCellsPro.Application.Interfaces;
using StemCellsPro.Infrastructure.Data;
using StemCellsPro.Domain.Models;

namespace StemCellsPro.Infrastructure.Repositories
{
    public class AttachmentsRepository : IAttachmentsRepository
    {
        private readonly DapperContext _context;

        public AttachmentsRepository(DapperContext context)
        {
            _context = context;
        }

        public async Task<IEnumerable<FormAttachment>> GetAttachmentsAsync(string formName, int formId)
        {
            using var connection = _context.CreateConnection();
            return await connection.QueryAsync<FormAttachment>(
                "SELECT * FROM dbo.FormAttachments WHERE FormName = @FormName AND FormId = @FormId ORDER BY UploadedAt DESC",
                new { FormName = formName, FormId = formId });
        }

        public async Task<FormAttachment> GetAttachmentByIdAsync(Guid attachmentId)
        {
            using var connection = _context.CreateConnection();
            return await connection.QuerySingleOrDefaultAsync<FormAttachment>(
                "SELECT * FROM dbo.FormAttachments WHERE AttachmentId = @AttachmentId",
                new { AttachmentId = attachmentId });
        }

        public async Task AddAttachmentAsync(FormAttachment attachment)
        {
            using var connection = _context.CreateConnection();
            var sql = @"
                INSERT INTO dbo.FormAttachments (AttachmentId, FormName, FormId, MayanDocumentId, FileName, ContentType, FileSize, UploadedBy, UploadedAt, Status, CreatedAt, UpdatedAt) VALUES (@AttachmentId, @FormName, @FormId, @MayanDocumentId, @FileName, @ContentType, @FileSize, @UploadedBy, @UploadedAt, @Status, @CreatedAt, @UpdatedAt)";
            
            await connection.ExecuteAsync(sql, attachment);
        }

        public async Task DeleteAttachmentAsync(Guid attachmentId)
        {
            using var connection = _context.CreateConnection();
            await connection.ExecuteAsync(
                "DELETE FROM dbo.FormAttachments WHERE AttachmentId = @AttachmentId",
                new { AttachmentId = attachmentId });
        }

        
    }
}


