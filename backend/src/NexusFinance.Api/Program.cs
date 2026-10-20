using System.Text;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using NexusFinance.Api.Endpoints;
using NexusFinance.Api.Exceptions;
using NexusFinance.Api.Security;
using NexusFinance.Application;
using NexusFinance.Application.Abstractions;
using NexusFinance.Infrastructure;
using NexusFinance.Infrastructure.Persistence;

var port = Environment.GetEnvironmentVariable("PORT");
if (!string.IsNullOrWhiteSpace(port))
{
    Environment.SetEnvironmentVariable("ASPNETCORE_URLS", $"http://0.0.0.0:{port}");
}

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddInfrastructure();
builder.Services.AddApplication();
builder.Services.Configure<JwtOptions>(builder.Configuration.GetSection(JwtOptions.SectionName));
builder.Services.AddSingleton<ITokenService, JwtTokenService>();
builder.Services.ConfigureHttpJsonOptions(options =>
{
    options.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

var jwt = builder.Configuration.GetSection(JwtOptions.SectionName).Get<JwtOptions>() ?? new JwtOptions();
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidateLifetime = true,
            ValidIssuer = jwt.Issuer,
            ValidAudience = jwt.Audience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwt.Key)),
            ClockSkew = TimeSpan.FromMinutes(1)
        };
    });
builder.Services.AddAuthorization();
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
app.UseAuthentication();
app.UseAuthorization();
app.UseStaticFiles();
app.MapHealthEndpoints();
app.MapApiEndpoints();

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

if (!app.Environment.IsEnvironment("Testing"))
{
    await using var scope = app.Services.CreateAsyncScope();
    await scope.ServiceProvider.GetRequiredService<MigrationRunner>().ApplyAsync(CancellationToken.None);
    await scope.ServiceProvider.GetRequiredService<DemoSeeder>().SeedAsync(CancellationToken.None);
}

app.Run();

public partial class Program;
