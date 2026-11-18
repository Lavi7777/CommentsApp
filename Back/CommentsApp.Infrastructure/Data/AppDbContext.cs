using CommentsApp.Domain.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Text;

namespace CommentsApp.Infrastructure.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<AppUser> Users => Set<AppUser>();
        public DbSet<Comment> Comments => Set<Comment>();
        public DbSet<Attachment> Attachments => Set<Attachment>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<AppUser>(e =>
            {
                e.ToTable("users");
                e.Property(x => x.UserName).HasMaxLength(50).IsRequired();
                e.Property(x => x.Email).HasMaxLength(254).IsRequired();
                e.Property(x => x.HomePage).HasMaxLength(2048);
                e.Property(x => x.IpAddress).HasMaxLength(45).IsRequired();
                e.Property(x => x.UserAgent).HasMaxLength(512).IsRequired();
                e.HasIndex(x => new { x.UserName, x.Email }).IsUnique();
            });

            modelBuilder.Entity<Comment>(e =>
            {
                e.ToTable("comments");
                e.Property(x => x.Text).HasMaxLength(5000).IsRequired();

                e.HasOne(x => x.User)
                    .WithMany(u => u.Comments)
                    .HasForeignKey(x => x.UserId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.Parent)
                    .WithMany(c => c.Replies)
                    .HasForeignKey(x => x.ParentId)
                    .OnDelete(DeleteBehavior.Restrict);

                e.HasIndex(x => x.ParentId);
                e.HasIndex(x => x.CreatedAt);
            });

            modelBuilder.Entity<Attachment>(e =>
            {
                e.ToTable("attachments");
                e.Property(x => x.OriginalFileName).HasMaxLength(255).IsRequired();
                e.Property(x => x.StoredFileName).HasMaxLength(255).IsRequired();

                e.HasOne(x => x.Comment)
                    .WithOne(c => c.Attachment)
                    .HasForeignKey<Attachment>(x => x.CommentId)
                    .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
