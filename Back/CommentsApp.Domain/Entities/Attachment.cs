using System;
using System.Collections.Generic;
using System.Text;

namespace CommentsApp.Domain.Entities
{
    public enum AttachmentType
    {
        Image = 1,
        Text = 2
    }

    public class Attachment
    {
        public Guid Id { get; set; }
        public AttachmentType Type { get; set; }
        public string OriginalFileName { get; set; } = null!;
        public string StoredFileName { get; set; } = null!;
        public long SizeBytes { get; set; }

        public Guid CommentId { get; set; }
        public Comment Comment { get; set; } = null!;
    }
}
