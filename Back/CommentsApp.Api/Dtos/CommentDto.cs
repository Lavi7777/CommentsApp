namespace CommentsApp.Api.Dtos;

public class CommentDto
{
    public Guid Id { get; set; }
    public Guid? ParentId { get; set; }
    public string UserName { get; set; } = null!;
    public string Email { get; set; } = null!;
    public string? HomePage { get; set; }
    public string Text { get; set; } = null!;
    public DateTime CreatedAt { get; set; }
    public AttachmentDto? Attachment { get; set; }
    public List<CommentDto> Replies { get; set; } = new();
}