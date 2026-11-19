using CommentsApp.Domain.Entities;

namespace CommentsApp.Domain.Models;

public record StoredAttachment(
    AttachmentType Type,
    string OriginalFileName,
    string StoredFileName,
    long SizeBytes);

public record AttachmentSaveResult(bool IsValid, StoredAttachment? Attachment, string? Error);