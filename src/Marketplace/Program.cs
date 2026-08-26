using Microsoft.AspNetCore.Mvc;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddOpenApi();

var app = builder.Build();

if (app.Environment.IsDevelopment())
{
    app.MapOpenApi();
}

app.UseHttpsRedirection();

// ============================================================
// Port: 5003 | Owner: shehasi (IT24610783)
//====================================================

// GET /marketplace/health — service health check
app.MapGet("/marketplace/health", () =>
{
    return Results.Ok(new
    {
        service = "Marketplace Service",
        status = "healthy",
        timestamp = DateTime.UtcNow,
        version = "1.0.0"
    });
})
.WithName("GetMarketplaceHealth")
.WithOpenApi();

// GET /marketplace/listings — browse refurbished catalog
app.MapGet("/marketplace/listings", () =>
{
    return Results.Ok(new
    {
        listings = new[]
        {
            new { id = 1, name = "Refurbished Dell Laptop", category = "Laptop", price = 45000, stock = 5, warrantyMonths = 6 },
            new { id = 2, name = "Refurbished iPhone 13", category = "Mobile", price = 28000, stock = 12, warrantyMonths = 3 },
            new { id = 3, name = "Samsung Monitor 24\"", category = "Monitor", price = 12000, stock = 8, warrantyMonths = 1 }
        },
        count = 3
    });
})
.WithName("GetListings")
.WithOpenApi();

// GET /marketplace/listings/{id} — get listing by ID
app.MapGet("/marketplace/listings/{id}", (int id) =>
{
    if (id <= 0)
        return Results.BadRequest("Invalid listing ID.");

    return Results.Ok(new
    {
        id = id,
        name = "Refurbished HP Laptop 15",
        category = "Laptop",
        description = "Refurbished laptop in good condition. 8GB RAM, 256GB SSD.",
        price = 38000,
        stock = 3,
        warrantyMonths = 6,
        sellerId = 201,
        createdAt = DateTime.UtcNow.AddDays(-10)
    });
})
.WithName("GetListingById")
.WithOpenApi();

app.Run();
