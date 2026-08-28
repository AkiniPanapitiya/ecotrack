using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Models;
using EcoTrack.LogisticsService.Repositories;

namespace EcoTrack.LogisticsService.Services;

public interface IPickupService
{
    Task<(bool Success, int StatusCode, string Message, PickupRequestDto? Data)> CreatePickupRequestAsync(
        Guid userId, CreatePickupRequestDto dto, CancellationToken cancellationToken = default);
    Task<PickupRequestDto?> GetPickupByIdAsync(Guid id, CancellationToken cancellationToken = default);
    Task<IEnumerable<PickupRequestDto>> GetPickupsByUserAsync(Guid userId, CancellationToken cancellationToken = default);
}

public class PickupService : IPickupService
{
    private readonly IPickupRepository _pickupRepository;

    public PickupService(IPickupRepository pickupRepository)
    {
        _pickupRepository = pickupRepository;
    }

    public async Task<(bool Success, int StatusCode, string Message, PickupRequestDto? Data)> CreatePickupRequestAsync(
        Guid userId, CreatePickupRequestDto dto, CancellationToken cancellationToken = default)
    {
        if (dto.EstimatedWeightKg <= 0 || dto.EstimatedWeightKg > 10000)
        {
            return (false, 400, "Estimated weight must be between 0.1 kg and 10,000 kg.", null);
        }

        if (string.IsNullOrWhiteSpace(dto.PickupAddress))
        {
            return (false, 400, "Pickup address is required.", null);
        }

        if (dto.PreferredDate.Date < DateTime.UtcNow.Date)
        {
            return (false, 400, "Preferred pickup date cannot be in the past.", null);
        }

        var pickupRequest = new PickupRequest
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            Category = dto.Category.Trim(),
            EstimatedWeightKg = dto.EstimatedWeightKg,
            PickupAddress = dto.PickupAddress.Trim(),
            ContactPhone = dto.ContactPhone.Trim(),
            PreferredDate = dto.PreferredDate,
            TimeSlot = dto.TimeSlot.Trim(),
            SpecialInstructions = dto.SpecialInstructions?.Trim(),
            Status = "Pending",
            CreatedAt = DateTime.UtcNow,
            UpdatedAt = DateTime.UtcNow,
            Items = dto.Items?.Select(i => new PickupItem
            {
                Id = Guid.NewGuid(),
                ItemName = i.ItemName.Trim(),
                Quantity = i.Quantity,
                ItemCondition = i.ItemCondition,
                EstimatedWeightKg = i.EstimatedWeightKg
            }).ToList() ?? new List<PickupItem>()
        };

        var created = await _pickupRepository.CreatePickupAsync(pickupRequest, cancellationToken);
        if (!created)
        {
            return (false, 500, "Failed to schedule pickup request.", null);
        }

        var resultDto = MapToDto(pickupRequest);
        return (true, 201, "E-waste pickup request scheduled successfully.", resultDto);
    }

    public async Task<PickupRequestDto?> GetPickupByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        var request = await _pickupRepository.GetByIdAsync(id, cancellationToken);
        return request != null ? MapToDto(request) : null;
    }

    public async Task<IEnumerable<PickupRequestDto>> GetPickupsByUserAsync(Guid userId, CancellationToken cancellationToken = default)
    {
        var requests = await _pickupRepository.GetByUserIdAsync(userId, cancellationToken);
        return requests.Select(MapToDto);
    }

    private static PickupRequestDto MapToDto(PickupRequest model)
    {
        return new PickupRequestDto
        {
            Id = model.Id,
            UserId = model.UserId,
            Category = model.Category,
            EstimatedWeightKg = model.EstimatedWeightKg,
            PickupAddress = model.PickupAddress,
            ContactPhone = model.ContactPhone,
            PreferredDate = model.PreferredDate,
            TimeSlot = model.TimeSlot,
            SpecialInstructions = model.SpecialInstructions,
            Status = model.Status,
            CreatedAt = model.CreatedAt,
            UpdatedAt = model.UpdatedAt,
            Items = model.Items.Select(i => new PickupItemDto
            {
                Id = i.Id,
                ItemName = i.ItemName,
                Quantity = i.Quantity,
                ItemCondition = i.ItemCondition,
                EstimatedWeightKg = i.EstimatedWeightKg
            }).ToList()
        };
    }
}
