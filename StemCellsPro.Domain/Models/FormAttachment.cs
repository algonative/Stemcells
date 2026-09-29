namespace StemCellsPro.Domain.Models
{
    public class FormAttachment
    {
        public Guid AttachmentId { get; set; }
        public int FormId { get; set; }
        public string FormName { get; set; }
        public string MayanDocumentId { get; set; }
        public string FileName { get; set; }
        public string ContentType { get; set; }
        public long FileSize { get; set; }
        public string UploadedBy { get; set; }
        public DateTime UploadedAt { get; set; }
        public string Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }
}

