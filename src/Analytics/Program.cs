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

// =====================================================
// Port: 5004 | Owner: dilanka (IT24610796)
// ============================================================

// GET /analytics/health — Service health check
app.MapGet("/analytics/health", () =>
{
    return Results.Ok(new
    {
        service = "Analytics Service",
        status = "healthy",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    });
})
.WithName("GetAnalyticsHealth")
.WithOpenApi();

// GET /analytics/impact/summary — Dummy environmental impact summary
app.MapGet("/analytics/impact/summary", () =>
{
    return Results.Ok(new
    {
        totalCo2DivertedKg = 12500.5,
        totalHeavyMetalsDivertedKg = 340.2,
        disposalCertificatesIssued = 1847,
        activeAlertRules = 3,
        lastUpdated = DateTime.UtcNow
    });
})
.WithName("GetImpactSummary")
.WithOpenApi();

// GET /analytics/disposal-certificates/{certId} — Get a specific certificate
app.MapGet("/analytics/disposal-certificates/{certId}", (int certId) =>
{
    if (certId <= 0)
        return Results.BadRequest("Invalid certificate ID.");

    return Results.Ok(new
    {
        certId = certId,
        userId = 1001,
        materialType = "E-Waste",
        co2OffsetKg = 12.5,
        issuedDate = DateTime.UtcNow.AddDays(-30),
        status = "Active"
    });
})
.WithName("GetDisposalCertificate")
.WithOpenApi();

app.Run();

// ============================================================
// Models
// ============================================================

record WeatherForecast(DateOnly Date, int TemperatureC, string? Summary)
{
    public int TemperatureF => 32 + (int)(TemperatureC / 0.5556);
}
