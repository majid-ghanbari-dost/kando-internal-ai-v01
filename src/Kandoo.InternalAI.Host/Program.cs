using Kandoo.InternalAI.Application;
using Kandoo.InternalAI.Core;
using Kandoo.InternalAI.Infrastructure;

var builder = WebApplication.CreateBuilder(args);
builder.Services.AddSingleton<IStoreReadGateway, FakeStoreReadGateway>();
builder.Services.AddSingleton<IAssistantModelProvider, FakeModelProvider>();
builder.Services.AddSingleton<IAssistantTool, ProductLookupTool>();
builder.Services.AddSingleton<IAssistantToolRegistry, AssistantToolRegistry>();
builder.Services.AddSingleton<AssistantService>();

var app = builder.Build();
app.MapGet("/health", () => Results.Ok(new { status = "ok", mode = "fixture" }));
app.MapPost("/assistant/ask", async (AssistantRequest request, AssistantService assistant, CancellationToken cancellationToken) =>
{
    try { return Results.Ok(await assistant.AskAsync(request, cancellationToken)); }
    catch (ArgumentException exception) { return Results.BadRequest(new { error = exception.Message }); }
});
app.Run();

public partial class Program { }
