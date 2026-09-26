using System.Text;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using EcoTrack.MarketplaceService.Data;
using EcoTrack.MarketplaceService.Repositories;
using EcoTrack.MarketplaceService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.PropertyNameCaseInsensitive = true;
    });
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Configure CORS — allow frontend origins (Vite dev server)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowFrontend", policy =>
    {
        policy.WithOrigins(
                "http://localhost:3000",
                "http://localhost:5173",
                "http://127.0.0.1:5173",
                "http://localhost:5174",
                "http://127.0.0.1:5174")
            .AllowAnyHeader()
            .AllowAnyMethod()
            .AllowCredentials();
    });
});

// Configure JWT Authentication (same key as Identity service — tokens are shared)
var jwtSecretKey = builder.Configuration["Jwt:SecretKey"]
    ?? builder.Configuration["Jwt:Key"]
    ?? "EcoTrack_Super_Secret_Key_For_Jwt_Auth_2026_SE3022_CaseStudy!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "EcoTrack.IdentityService";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "EcoTrack.ClientApps";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecretKey)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ValidateLifetime = true,
        ClockSkew = TimeSpan.Zero
    };
});
builder.Services.AddAuthorization();

// Register DB connection factory — connection string from appsettings
var connectionString = builder.Configuration.GetConnectionString("MarketplaceDb")
    ?? "Server=localhost;Database=ecotrack_marketplace_db;Uid=root;Pwd=;";
builder.Services.AddSingleton<IDbConnectionFactory>(new DbConnectionFactory(connectionString));

// Register valuation services
builder.Services.AddScoped<IValuationRepository, ValuationRepository>();
builder.Services.AddScoped<IValuationService, ValuationService>();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

// app.UseHttpsRedirection(); // Disabled — browser CORS preflight fails on HTTP redirect

app.UseCors("AllowFrontend");

// Auth middleware — must come before MapControllers
app.UseAuthentication();
app.UseAuthorization();

// =============================================
// Port: 5003 | Owner: sehenasi (IT24610783)
//======================================

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

// GET /marketplace/listings — browse refurbished catalog (placeholder)
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

// Map controller routes
app.MapControllers();

app.Run();
