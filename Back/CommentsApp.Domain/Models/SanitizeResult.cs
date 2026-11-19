using System;
using System.Collections.Generic;
using System.Text;

namespace CommentsApp.Domain.Models
{
    
        public record SanitizeResult(bool IsValid, string Html, string? Error);
    
}
