using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Models;
using EcoTrack.LogisticsService.Repositories;

namespace EcoTrack.LogisticsService.Services;

public class DriverService : IDriverService
{
    private const string ScheduledStatus = "Scheduled";

    private readonly IDriverRepository _driverRepository;
    private readonly IPickupRepository _pickupRepository;

    public DriverService(IDriverRepository driverRepository, IPickupRepository pickupRepository)
    {
        _driverRepository = driverRepository;
        _pickupRepository = pickupRepository;
    }

    public async Task<(bool Success, int StatusCode, string Message, DriverDto? Driver)> AddDriverAsync(
        Guid recyclerId, CreateDriverRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(dto.FullName) || string.IsNullOrWhiteSpace(dto.Phone))
        {
            return (false, 400, "Driver name and phone number are required.", null);
        }

        var driver = new Driver
        {
            RecyclerId = recyclerId,
            FullName = dto.FullName.Trim(),
            Phone = dto.Phone.Trim(),
            VehicleNumber = string.IsNullOrWhiteSpace(dto.VehicleNumber) ? null : dto.VehicleNumber.Trim()
        };

        var created = await _driverRepository.CreateAsync(driver, cancellationToken);
        if (!created)
        {
            return (false, 500, "Could not save the driver. Please try again.", null);
        }

        return (true, 201, "Driver added.", ToDto(driver));
    }

    public async Task<IEnumerable<DriverDto>> GetMyDriversAsync(
        Guid recyclerId, CancellationToken cancellationToken = default)
    {
        var drivers = await _driverRepository.GetActiveByRecyclerIdAsync(recyclerId, cancellationToken);
        return drivers.Select(ToDto);
    }

    public async Task<(bool Success, int StatusCode, string Message, PickupDriverDto? Assignment)> AssignDriverAsync(
        Guid recyclerId, Guid pickupId, Guid driverId, CancellationToken cancellationToken = default)
    {
        if (driverId == Guid.Empty)
        {
            return (false, 400, "Driver ID is required.", null);
        }

        var pickup = await _pickupRepository.GetByIdAsync(pickupId, cancellationToken);
        if (pickup == null)
        {
            return (false, 404, "Pickup request not found.", null);
        }

        if (pickup.RecyclerId != recyclerId)
        {
            return (false, 403, "You can only assign drivers to your own pickups.", null);
        }

        if (!string.Equals(pickup.Status, ScheduledStatus, StringComparison.OrdinalIgnoreCase))
        {
            return (false, 400, "Drivers can only be assigned to scheduled pickups.", null);
        }

        var myDrivers = (await _driverRepository.GetActiveByRecyclerIdAsync(recyclerId, cancellationToken)).ToList();
        if (myDrivers.Count == 0)
        {
            return (false, 400, "No drivers available. Please add a driver first.", null);
        }

        var driver = myDrivers.FirstOrDefault(d => d.Id == driverId);
        if (driver == null)
        {
            return (false, 404, "Driver not found.", null);
        }

        var assigned = await _driverRepository.AssignToPickupAsync(pickupId, driverId, cancellationToken);
        if (!assigned)
        {
            return (false, 500, "Could not assign the driver. Please try again.", null);
        }

        return (true, 200, "Driver assigned.", ToAssignmentDto(pickupId, driver));
    }

    public async Task<(bool Success, int StatusCode, string Message, PickupDriverDto? Assignment)> GetPickupDriverAsync(
        Guid pickupId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var pickup = await _pickupRepository.GetByIdAsync(pickupId, cancellationToken);
        if (pickup == null)
        {
            return (false, 404, "Pickup request not found.", null);
        }

        if (pickup.UserId != requestingUserId && pickup.RecyclerId != requestingUserId)
        {
            return (false, 403, "You are not allowed to view this pickup.", null);
        }

        var driver = await _driverRepository.GetAssignedDriverAsync(pickupId, cancellationToken);
        if (driver == null)
        {
            return (false, 404, "No driver assigned to this pickup yet.", null);
        }

        return (true, 200, "Driver found.", ToAssignmentDto(pickupId, driver));
    }

    private static DriverDto ToDto(Driver driver) => new()
    {
        Id = driver.Id,
        FullName = driver.FullName,
        Phone = driver.Phone,
        VehicleNumber = driver.VehicleNumber
    };

    private static PickupDriverDto ToAssignmentDto(Guid pickupId, Driver driver) => new()
    {
        PickupId = pickupId,
        DriverId = driver.Id,
        DriverName = driver.FullName,
        DriverPhone = driver.Phone,
        VehicleNumber = driver.VehicleNumber
    };
}