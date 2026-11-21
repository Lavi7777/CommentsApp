using System.Text.Json.Serialization;
using CommentsApp.Api.Handlers;
using CommentsApp.Api.Hubs;
using CommentsApp.Domain.Abstractions;
using CommentsApp.Domain.Events;
using CommentsApp.Infrastructure.Data;
using CommentsApp.Infrastructure.Events;
using CommentsApp.Infrastructure.Handlers;
using CommentsApp.Infrastructure.Services;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.FileProviders;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllers()
    .AddJsonOptions(options =>
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter()));
builder.Services.AddOpenApi();
builder.Services.AddSignalR();

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("Default")));

builder.Services.AddMemoryCache();
builder.Services.AddSingleton<ICommentTextSanitizer, CommentTextSanitizer>();
builder.Services.AddSingleton<ICaptchaService, CaptchaService>();
builder.Services.AddSingleton<ICommentsCache, CommentsCache>();

builder.Services.AddScoped<IEventPublisher, EventPublisher>();
builder.Services.AddScoped<IDomainEventHandler<CommentCreatedEvent>, CommentCreatedCacheInvalidationHandler>();
builder.Services.AddScoped<IDomainEventHandler<CommentCreatedEvent>, CommentCreatedBroadcastHandler>();

var uploadsPath = Path.GetFullPath(
    builder.Configuration["Uploads:Path"]
    ?? Path.Combine(builder.Environment.ContentRootPath, "uploads"));
Directory.CreateDirectory(uploadsPath);
builder.Services.AddSingleton<IAttachmentStorage>(_ => new AttachmentStorage(uploadsPath));

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}
else
{
    app.UseHttpsRedirection();
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(uploadsPath),
    RequestPath = "/uploads",
    OnPrepareResponse = ctx =>
        ctx.Context.Response.Headers["X-Content-Type-Options"] = "nosniff"
});

app.UseAuthorization();
app.MapControllers();
app.MapHub<CommentsHub>("/hubs/comments");

app.Run();