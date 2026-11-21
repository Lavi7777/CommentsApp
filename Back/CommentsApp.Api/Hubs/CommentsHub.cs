using Microsoft.AspNetCore.SignalR;

namespace CommentsApp.Api.Hubs;

public class CommentsHub : Hub
{
    public const string CommentCreated = "commentCreated";
}