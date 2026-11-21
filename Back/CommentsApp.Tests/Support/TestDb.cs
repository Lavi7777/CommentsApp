using CommentsApp.Domain.Entities;
using CommentsApp.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;

namespace CommentsApp.Tests.Support;

public static class TestDb
{
    public static AppDbContext Create()
    {
        var options = new DbContextOptionsBuilder<AppDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        return new AppDbContext(options);
    }

    public static AppUser AddUser(AppDbContext db, string userName, string email)
    {
        var user = new AppUser
        {
            Id = Guid.NewGuid(),
            UserName = userName,
            Email = email,
            IpAddress = "127.0.0.1",
            UserAgent = "tests",
            CreatedAt = DateTime.UtcNow
        };

        db.Users.Add(user);
        db.SaveChanges();
        return user;
    }

    public static Comment AddComment(
        AppDbContext db,
        AppUser user,
        string text,
        DateTime createdAt,
        Guid? parentId = null)
    {
        var comment = new Comment
        {
            Id = Guid.NewGuid(),
            UserId = user.Id,
            Text = text,
            CreatedAt = createdAt,
            ParentId = parentId
        };

        db.Comments.Add(comment);
        db.SaveChanges();
        return comment;
    }
}