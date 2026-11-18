using System;
using System.Collections.Generic;
using System.Text;

namespace CommentsApp.Domain.Entities
{
    public class Comment
    {
        public Guid Id { get; set; }
        public string Text { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public Guid UserId { get; set; }
        public AppUser User { get; set; } = null!;

        public Guid? ParentId { get; set; }
        public Comment? Parent { get; set; }
        public List<Comment> Replies { get; set; } = new();

        public Attachment? Attachment { get; set; }
    }
}
