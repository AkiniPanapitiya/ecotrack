using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

//===================================================
// Port: 5002 | Owner: diloosha (IT24610798)
//============================================================

// GET /logistics/health — Service health check
app.MapGet("/logistics/health", () =>
{
    return Results.Ok(new
    {
        service = "Logistics Service",
        status = "healthy",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    });
})
.WithName("GetLogisticsHealth")
.WithOpenApi();

// GET /logistics/pickup-requests — list pickup requests
app.MapGet("/logistics/pickup-requests", () =>
{
    return Results.Ok(new
    {
        pickupRequests = new[]
        {
            new { id = 1, userId = 101, address = "123 Main St, Colombo", status = "Requested", scheduledDate = DateTime.UtcNow.AddDays(2) },
            new { id = 2, userId = 102, address = "45 Galle Rd, Moratuwa", status = "PickedUp", scheduledDate = DateTime.UtcNow.AddDays(-1) }
        },
        count = 2
    });
})
.WithName("GetPickupRequests")
.WithOpenApi();

// GET /logistics/pickup-requests/{id} — get pickup by ID
app.MapGet("/logistics/pickup-requests/{id}", (int id) =>
{
    if (id <= 0)
        return Results.BadRequest("Invalid pickup request ID.");

    return Results.Ok(new
    {
        id = id,
        userId = 1001,
        items = new[] { new { category = "Laptop", weightKg = 2.5 }, new { category = "Mobile", weightKg = 0.3 } },
        address = "Sample Address, Colombo",
        status = "Requested",
        scheduledDate = DateTime.UtcNow.AddDays(3),
        qrToken = "QR-" + Guid.NewGuid().ToString().Substring(0, 8).ToUpper()
    });
})
.WithName("GetPickupRequestById")
.WithOpenApi();

// POST /logistics/pickup-requests — create pickup request
app.MapPost("/logistics/pickup-requests", (dynamic body) =>
{
    // Placeholder — will connect to database later
    return Results.Created("/logistics/pickup-requests/999", new
    {
        id = 999,
        userId = 1001,
        address = "New Address",
        status = "Requested",
        scheduledDate = DateTime.UtcNow.AddDays(1),
        message = "Pickup request created (placeholder)."
    });
})
.WithName("CreatePickupRequest")
.WithOpenApi();

app.Run();
