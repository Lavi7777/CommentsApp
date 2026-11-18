using System;
using System.Collections.Generic;
using System.Text;

namespace CommentsApp.Domain.Entities
{
    public class AppUser
    {
        public Guid Id { get; set; }
        public string UserName { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? HomePage { get; set; }
        public string IpAddress { get; set; } = null!;
        public string UserAgent { get; set; } = null!;
        public DateTime CreatedAt { get; set; }

        public List<Comment> Comments { get; set; } = new();
    }
}
