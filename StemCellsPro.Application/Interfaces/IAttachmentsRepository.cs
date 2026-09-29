using StemCellsPro.Domain.Models;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace StemCellsPro.Application.Interfaces
{
    public interface IAttachmentsRepository
    {
        Task<IEnumerable<FormAttachment>> GetAttachmentsAsync(string formName, int formId);
        Task<FormAttachment> GetAttachmentByIdAsync(Guid attachmentId);
        Task AddAttachmentAsync(FormAttachment attachment);
        Task DeleteAttachmentAsync(Guid attachmentId);
        
    }
}

