using Lms.ContentService.Application.Interfaces;
using System.Net.Http.Json;

namespace Lms.ContentService.Infrastructure.Clients;

public class FileServiceClient : IFileServiceClient
{
    private readonly HttpClient _httpClient;
    public FileServiceClient(HttpClient httpClient) => _httpClient = httpClient;

    public async Task<bool> FileExistsAsync(Guid fileId)
    {
        var response = await _httpClient.GetAsync($"api/internal/files/{fileId}/exists");
        return response.IsSuccessStatusCode;
    }

    public async Task<FileMetadataDto?> GetFileMetadataAsync(Guid fileId)
    {
        var response = await _httpClient.GetAsync($"api/internal/files/{fileId}/metadata");
        return response.IsSuccessStatusCode ? await response.Content.ReadFromJsonAsync<FileMetadataDto>() : null;
    }
}