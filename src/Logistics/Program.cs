using EcoTrack.LogisticsService.Data;
using EcoTrack.LogisticsService.Repositories;
using EcoTrack.LogisticsService.Services;

var builder = WebApplication.CreateBuilder(args);

// Add Controllers
builder.Services.AddControllers();

// Configure CORS
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

// Configure ADO.NET Data Services & Repositories (100% Parameterized SQL, Zero-ORM)
builder.Services.AddSingleton<IDbConnectionFactory, DbConnectionFactory>();
builder.Services.AddScoped<IPickupRepository, PickupRepository>();
builder.Services.AddScoped<IPickupService, PickupService>();

// Configure Swagger / OpenAPI
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Configure HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseCors("AllowFrontend");

app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/logistics/health", () => Results.Ok(new
{
    status = "healthy",
    service = "Logistics Service (E-Waste Pickup Scheduling - ECO-15)",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

app.Run();
