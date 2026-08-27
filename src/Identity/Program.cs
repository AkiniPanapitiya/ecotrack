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

// ====================================================
// Port: 5001 | Owner: ankini (IT24610790)
// ============================================================

// GET /identity/health — service health check
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

// GET /identity/recyclers — list recycler profiles (placeholder)
app.MapGet("/identity/recyclers", () =>
{
    return Results.Ok(new
    {
        recyclers = new[]
        {
            new { id = 1, companyName = "GreenTech Recycles Ltd", status = "Active", kycVerified = true },
            new { id = 2, companyName = "EcoCycle Handling Pvt Ltd", status = "Pending", kycVerified = false }
        },
        count = 2
    });
})
.WithName("GetRecyclers")
.WithOpenApi();

// GET /identity/recyclers/{id} — get recycler by ID
app.MapGet("/identity/recyclers/{id}", (int id) =>
{
    if (id <= 0)
        return Results.BadRequest("Invalid recycler ID.");

    return Results.Ok(new
    {
        id = id,
        companyName = "Sample Recycler Co.",
        email = "contact@sample.com",
        status = "Active",
        kycVerified = true,
        registrationDate = DateTime.UtcNow.AddMonths(-6)
    });
})
.WithName("GetRecyclerById")
.WithOpenApi();

app.Run();
