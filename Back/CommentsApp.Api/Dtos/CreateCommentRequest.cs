using System.ComponentModel.DataAnnotations;

namespace CommentsApp.Api.Dtos;

public class CreateCommentRequest
{
    [Required]
    [StringLength(50)]
    [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "UserName: only Latin letters and digits.")]
    public string UserName { get; set; } = null!;

    [Required]
    [EmailAddress]
    [StringLength(254)]
    public string Email { get; set; } = null!;

    [Url]
    [StringLength(2048)]
    public string? HomePage { get; set; }

    [Required]
    [StringLength(5000)]
    public string Text { get; set; } = null!;

    public Guid? ParentId { get; set; }

    [Required]
    public string CaptchaId { get; set; } = null!;

    [Required]
    [StringLength(10)]
    [RegularExpression("^[a-zA-Z0-9]+$", ErrorMessage = "Captcha: only Latin letters and digits.")]
    public string CaptchaCode { get; set; } = null!;
}