using NexusFinance.Api.Endpoints;
using NexusFinance.Api.Exceptions;
using NexusFinance.Infrastructure;

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{port}");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddProblemDetails();
builder.Services.AddExceptionHandler<GlobalExceptionHandler>();

var allowedOrigins = builder.Configuration.GetSection("Cors:Origins").Get<string[]>() ?? [];
builder.Services.AddCors(options =>
{
    options.AddPolicy("Frontend", policy =>
    {
        policy.WithOrigins(allowedOrigins)
            .AllowAnyHeader()
            .AllowAnyMethod();
    });
});

builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages(async statusCodeContext =>
{
    var http = statusCodeContext.HttpContext;
    if (http.Response.StatusCode != StatusCodes.Status404NotFound || http.Response.HasStarted)
    {
        return;
    }

    await Results.Problem(
        title: "Recurso não encontrado",
        statusCode: StatusCodes.Status404NotFound,
        instance: http.Request.Path
    ).ExecuteAsync(http);
});

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("Frontend");
app.UseStaticFiles();
app.MapHealthEndpoints();

app.Map("/api/{**catchAll}", (HttpContext httpContext) =>
    Results.Problem(
        title: "Recurso não encontrado",
        statusCode: StatusCodes.Status404NotFound,
        instance: httpContext.Request.Path));

if (app.Environment.IsEnvironment("Testing"))
{
    app.MapGet("/api/test/throw", (HttpContext _) =>
    {
        throw new InvalidOperationException("falha de teste");
    });
}

var indexFile = Path.Combine(app.Environment.WebRootPath ?? string.Empty, "index.html");
if (!app.Environment.IsEnvironment("Testing") && File.Exists(indexFile))
{
    app.MapFallbackToFile("index.html");
}

app.Run();

public partial class Program;
