using System.ComponentModel.DataAnnotations;

namespace CommentsApp.Api.Dtos;

public class PreviewRequest
{
    [Required]
    [StringLength(5000)]
    public string Text { get; set; } = null!;
}