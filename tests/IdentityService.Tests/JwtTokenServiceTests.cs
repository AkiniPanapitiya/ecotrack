using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using EcoTrack.IdentityService.Models;
using EcoTrack.IdentityService.Services;
using Microsoft.Extensions.Configuration;
using Xunit;

namespace EcoTrack.IdentityService.Tests;

public class JwtTokenServiceTests
{
    private readonly IConfiguration _configuration;
    private readonly JwtTokenService _jwtTokenService;

    public JwtTokenServiceTests()
    {
        var inMemorySettings = new Dictionary<string, string?>
        {
            {"Jwt:SecretKey", "Test_Secret_Key_At_Least_32_Bytes_Long_For_HmacSha256_Tests!"},
            {"Jwt:Issuer", "EcoTrack.IdentityService.Tests"},
            {"Jwt:Audience", "EcoTrack.ClientApps.Tests"},
            {"Jwt:ExpiryMinutes", "60"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _jwtTokenService = new JwtTokenService(_configuration);
    }

    [Fact]
    public void GenerateToken_ValidUser_ReturnsSignedJwtWithRoleClaims()
    {
        // Arrange
        var user = new User
        {
            Id = Guid.NewGuid(),
            FullName = "Akini Recycler",
            Email = "recycler@ecotrack.lk",
            Role = "Recycler"
        };

        // Act
        var (token, expiresAt) = _jwtTokenService.GenerateToken(user, "Pending");

        // Assert
        Assert.NotNull(token);
        Assert.True(expiresAt > DateTime.UtcNow);

        var handler = new JwtSecurityTokenHandler();
        var jwtSecurityToken = handler.ReadJwtToken(token);

        Assert.Equal("EcoTrack.IdentityService.Tests", jwtSecurityToken.Issuer);
        Assert.Contains(jwtSecurityToken.Claims, c => (c.Type == "role" || c.Type == ClaimTypes.Role) && c.Value == "Recycler");
        Assert.Contains(jwtSecurityToken.Claims, c => (c.Type == "unique_name" || c.Type == "name" || c.Type == ClaimTypes.Name) && c.Value == "Akini Recycler");
        Assert.Contains(jwtSecurityToken.Claims, c => (c.Type == "email" || c.Type == ClaimTypes.Email) && c.Value == "recycler@ecotrack.lk");
        Assert.Contains(jwtSecurityToken.Claims, c => c.Type == "verification_status" && c.Value == "Pending");
    }
}
