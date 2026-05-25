namespace Lms.ContentService.Application.Interfaces;

public interface IFileServiceClient
{
    Task<bool> FileExistsAsync(Guid fileId);
    Task<FileMetadataDto?> GetFileMetadataAsync(Guid fileId);
}

public class FileMetadataDto
{
    public Guid Id { get; set; }
    public string FileName { get; set; } = string.Empty;
    public string ContentType { get; set; } = string.Empty;
    public long FileSize { get; set; }
}