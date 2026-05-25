namespace Lms.ContentService.Application.DTOs;

public class AttachResourceRequest
{
    public Guid FileId { get; set; }
    public string ResourceType { get; set; } = "Document";
}