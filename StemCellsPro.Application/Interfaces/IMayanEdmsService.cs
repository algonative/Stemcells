namespace StemCellsPro.Application.Interfaces;

public interface IMayanEdmsService
{
    Task<string> UploadDocumentAsync(string fileName, byte[] fileData, string contentType, int documentTypeId = 1);
    Task<(byte[] FileBytes, string FileName, string MimeType)> DownloadDocumentAsync(string documentId);
}
