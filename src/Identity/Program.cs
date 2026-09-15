using System.Text;
using EcoTrack.IdentityService.Data;
using EcoTrack.IdentityService.Repositories;
using EcoTrack.IdentityService.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Security.Claims;
using System.IdentityModel.Tokens.Jwt;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using EcoTrack.IdentityService.Repositories;



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
builder.Services.AddScoped<IUserRepository, UserRepository>();
builder.Services.AddScoped<IAuditRepository, AuditRepository>();
builder.Services.AddScoped<ITokenBlacklistRepository, TokenBlacklistRepository>();
builder.Services.AddScoped<IPasswordResetTokenRepository, PasswordResetTokenRepository>();

// Configure Domain Services
builder.Services.AddScoped<IPasswordHasher, BcryptPasswordHasher>();
builder.Services.AddScoped<IAuthService, AuthService>();
builder.Services.AddScoped<IJwtTokenService, JwtTokenService>();
builder.Services.AddScoped<IProfileService, ProfileService>();

// Configure JWT Authentication
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
    options.Events = new JwtBearerEvents
    {
        OnTokenValidated = async context =>
        {
            var jti = context.Principal?.FindFirstValue(JwtRegisteredClaimNames.Jti);
            if (string.IsNullOrEmpty(jti))
            {
                context.Fail("Invalid token.");
                return;
            }

            var blacklistRepo = context.HttpContext.RequestServices
                .GetRequiredService<ITokenBlacklistRepository>();

            if (await blacklistRepo.IsBlacklistedAsync(jti))
            {
                context.Fail("This token has been logged out.");
            }
        }
    };
});

builder.Services.AddAuthorization();

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

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// Health check endpoint
app.MapGet("/identity/health", () => Results.Ok(new
{
    service = "Identity Service",
    status = "healthy",
    timestamp = DateTime.UtcNow,
    version = "1.0.0"
}));

app.Run();
