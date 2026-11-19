using CommentsApp.Domain.Entities;

namespace CommentsApp.Api.Dtos;

public class AttachmentDto
{
    public AttachmentType Type { get; set; }
    public string OriginalFileName { get; set; } = null!;
    public string Url { get; set; } = null!;
    public long SizeBytes { get; set; }
}