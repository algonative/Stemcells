using System;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Options;
using StemCellsPro.Application.Configuration;
using StemCellsPro.Application.Interfaces;

namespace StemCellsPro.Infrastructure.Services
{
    public class MayanEdmsService : IMayanEdmsService
    {
        private readonly HttpClient _httpClient;
        private readonly MayanEdmsSettings _settings;

        public MayanEdmsService(HttpClient httpClient, IOptions<MayanEdmsSettings> settings)
        {
            _httpClient = httpClient;
            _settings = settings.Value;

            if (!string.IsNullOrEmpty(_settings.Token))
            {
                _httpClient.DefaultRequestHeaders.Add("Authorization", $"Token {_settings.Token}");
            }

            var baseUrl = _settings.BaseUrl.EndsWith("/") ? _settings.BaseUrl : _settings.BaseUrl + "/";
            _httpClient.BaseAddress = new Uri(baseUrl);
        }

        public async Task<string> UploadDocumentAsync(string fileName, byte[] fileData, string contentType, int documentTypeId = 1)
        {
            var createDocPayload = new { document_type_id = documentTypeId };
            var createDocContent = new StringContent(JsonSerializer.Serialize(createDocPayload), Encoding.UTF8, "application/json");
            
            var createResponse = await _httpClient.PostAsync("api/v4/documents/", createDocContent);
            if (!createResponse.IsSuccessStatusCode)
            {
                var errContent = await createResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to create document stub in Mayan EDMS. Status: {createResponse.StatusCode}, Details: {errContent}");
            }

            var responseString = await createResponse.Content.ReadAsStringAsync();
            using var jsonDocument = JsonDocument.Parse(responseString);
            var documentId = jsonDocument.RootElement.GetProperty("id").GetRawText();

            using var fileMultipartContent = new MultipartFormDataContent();
            var fileByteArrayContent = new ByteArrayContent(fileData);
            fileByteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            
            fileMultipartContent.Add(new StringContent("replace"), "action_name");
            fileMultipartContent.Add(fileByteArrayContent, "file_new", fileName);
            
            var uploadResponse = await _httpClient.PostAsync($"api/v4/documents/{documentId}/files/", fileMultipartContent);
            
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errContent = await uploadResponse.Content.ReadAsStringAsync();
                throw new Exception($"Failed to upload file to document {documentId}. Status: {uploadResponse.StatusCode}, Details: {errContent}");
            }
            
            return documentId;
        }

        public async Task<(byte[] FileBytes, string FileName, string MimeType)> DownloadDocumentAsync(string documentId)
        {
            var filesResponse = await _httpClient.GetAsync($"api/v4/documents/{documentId}/files/");
            
            if (!filesResponse.IsSuccessStatusCode)
                throw new Exception($"Failed to fetch document files from Mayan EDMS. Status: {filesResponse.StatusCode}");

            var filesResponseString = await filesResponse.Content.ReadAsStringAsync();
            using var jsonDocument = JsonDocument.Parse(filesResponseString);
            var results = jsonDocument.RootElement.GetProperty("results");
            
            if (results.GetArrayLength() == 0)
                throw new Exception("No files found for this document in Mayan EDMS.");

            var firstFile = results[0];
            var fileId = firstFile.GetProperty("id").GetRawText();
            var fileName = firstFile.GetProperty("filename").GetString() ?? $"document_{documentId}";
            var mimeType = firstFile.GetProperty("mimetype").GetString() ?? "application/octet-stream";

            var downloadResponse = await _httpClient.GetAsync($"api/v4/documents/{documentId}/files/{fileId}/download/");
            
            if (!downloadResponse.IsSuccessStatusCode)
                throw new Exception($"Failed to download document file from Mayan EDMS. Status: {downloadResponse.StatusCode}");

            var fileBytes = await downloadResponse.Content.ReadAsByteArrayAsync();
            return (fileBytes, fileName, mimeType);
        }

        public async Task<string> UploadDocumentToSourceAsync(string fileName, byte[] fileData, string contentType)
        {
            using var fileMultipartContent = new MultipartFormDataContent();
            
            var fileByteArrayContent = new ByteArrayContent(fileData);
            fileByteArrayContent.Headers.ContentType = MediaTypeHeaderValue.Parse(contentType);
            
            fileMultipartContent.Add(new StringContent("1"), "document_type_id");
            fileMultipartContent.Add(fileByteArrayContent, "file", fileName);

            var uploadResponse = await _httpClient.PostAsync("api/v4/sources/1/documents/", fileMultipartContent);
            
            if (!uploadResponse.IsSuccessStatusCode)
            {
                var errorContent = await uploadResponse.Content.ReadAsStringAsync();
                throw new Exception($"Mayan Upload Failed. Status: {uploadResponse.StatusCode}, Details: {errorContent}");
            }

            var responseString = await uploadResponse.Content.ReadAsStringAsync();
            using var jsonDocument = JsonDocument.Parse(responseString);
            return jsonDocument.RootElement.GetProperty("id").GetRawText();
        }

        public async Task DeleteDocumentAsync(string mayanDocumentId)
        {
            var response = await _httpClient.DeleteAsync($"api/v4/documents/{mayanDocumentId}/");
            if (!response.IsSuccessStatusCode && response.StatusCode != System.Net.HttpStatusCode.NotFound)
            {
                throw new Exception($"Failed to delete document in Mayan. Status: {response.StatusCode}");
            }
        }
    }
}
