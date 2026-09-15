namespace EcoTrack.LogisticsService.Models;

public class PickupResult
{
    public bool Success { get; set; }
    public string Error { get; set; } = string.Empty;

    public static PickupResult Ok() => new PickupResult { Success = true };
    public static PickupResult Fail(string error) => new PickupResult { Success = false, Error = error };
}