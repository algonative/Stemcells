using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Options;
using StemCellsPro.Application.Configuration;
using StemCellsPro.Application.Interfaces;

namespace StemCellsPro.Infrastructure.Services;

public class MayanEdmsService : IMayanEdmsService
{
    private readonly HttpClient _httpClient;
    private readonly MayanEdmsSettings _settings;

    public MayanEdmsService(HttpClient httpClient, IOptions<MayanEdmsSettings> settings)
    {
        _httpClient = httpClient;
        _settings = settings.Value;

        // Configure basic authentication
        var authString = Convert.ToBase64String(Encoding.ASCII.GetBytes($"{_settings.Username}:{_settings.Password}"));
        _httpClient.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", authString);
        
        // Ensure BaseUrl ends with a slash
        var baseUrl = _settings.BaseUrl.EndsWith("/") ? _settings.BaseUrl : _settings.BaseUrl + "/";
        _httpClient.BaseAddress = new Uri(baseUrl);
    }

    public async Task<string> UploadDocumentAsync(string fileName, byte[] fileData, string contentType, int documentTypeId = 1)
    {
        // Step 1: Create Document Stub
        var createDocPayload = new { document_type_id = documentTypeId };
        var createDocContent = new StringContent(JsonSerializer.Serialize(createDocPayload), Encoding.UTF8, "application/json");
        
        var createResponse = await _httpClient.PostAsync("api/v4/documents/", createDocContent);
        if (!createResponse.IsSuccessStatusCode)
        {
            var errorContent = await createResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to create document stub in Mayan EDMS. Status: {createResponse.StatusCode}, Details: {errorContent}");
        }

        var responseString = await createResponse.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(responseString);
        var documentId = jsonDocument.RootElement.GetProperty("id").GetRawText();

        // Step 2: Upload File to the Document Stub
        using var fileMultipartContent = new MultipartFormDataContent();
        
        var fileByteArrayContent = new ByteArrayContent(fileData);
        fileByteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
        
        fileMultipartContent.Add(new StringContent("replace"), "action_name");
        fileMultipartContent.Add(fileByteArrayContent, "file_new", fileName);
        
        // Mayan v4 file upload endpoint requires action=1 (Keep local copy) or something? Usually just file_new is enough.
        // Wait, the API options for /files/ says it takes "file_new" as the file upload field. Let's try "file_new". If it fails, we will see in the error.
        var uploadResponse = await _httpClient.PostAsync($"api/v4/documents/{documentId}/files/", fileMultipartContent);
        
        if (!uploadResponse.IsSuccessStatusCode)
        {
            var errorContent = await uploadResponse.Content.ReadAsStringAsync();
            throw new Exception($"Failed to upload file to document {documentId}. Status: {uploadResponse.StatusCode}, Details: {errorContent}");
        }
        
        return documentId;
    }

    public async Task<(byte[] FileBytes, string FileName, string MimeType)> DownloadDocumentAsync(string documentId)
    {
        // Mayan EDMS v4 API for getting the latest file of a document usually involves querying the document files first.
        // For simplicity, we will fetch the document's file list and download the first one.
        var filesResponse = await _httpClient.GetAsync($"api/v4/documents/{documentId}/files/");
        
        if (!filesResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to fetch document files from Mayan EDMS. Status: {filesResponse.StatusCode}");
        }

        var filesResponseString = await filesResponse.Content.ReadAsStringAsync();
        using var jsonDocument = JsonDocument.Parse(filesResponseString);
        var results = jsonDocument.RootElement.GetProperty("results");
        
        if (results.GetArrayLength() == 0)
        {
            throw new Exception("No files found for this document in Mayan EDMS.");
        }

        var firstFile = results[0];
        var fileId = firstFile.GetProperty("id").GetRawText();
        var fileName = firstFile.GetProperty("filename").GetString() ?? $"document_{documentId}";
        var mimeType = firstFile.GetProperty("mimetype").GetString() ?? "application/octet-stream";

        var downloadResponse = await _httpClient.GetAsync($"api/v4/documents/{documentId}/files/{fileId}/download/");
        
        if (!downloadResponse.IsSuccessStatusCode)
        {
            throw new Exception($"Failed to download document file from Mayan EDMS. Status: {downloadResponse.StatusCode}");
        }

        var fileBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
        return (fileBytes, fileName, mimeType);
    }
}
