using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// ============================================================
// Identity Service — User Authentication & Access Management
// Port: 5001 | Owner: Rajapaksha (IT24610798)
// ============================================================

// GET /identity/health — Service health check
app.MapGet("/identity/health", () =>
{
    return Results.Ok(new
    {
        service = "Identity Service",
        status = "healthy",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    });
})
.WithName("GetIdentityHealth")
.WithOpenApi();

app.Run();
