using System.Globalization;
using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Events;
using EcoTrack.LogisticsService.Messaging;
using EcoTrack.LogisticsService.Models;
using EcoTrack.LogisticsService.Repositories;

namespace EcoTrack.LogisticsService.Services;

/// <summary>
/// ECO-26 / E4.2: Drop-off & Dispatch Record.
/// The assigned driver/recycler records the drop-off, which links a record to the
/// original pickup request and moves the pickup to 'Collected'.
/// </summary>
public class DropoffService : IDropoffService
{
    public const int MaxNotesLength = 1000;

    private readonly IDropoffRepository _dropoffRepository;
    private readonly IPickupRepository _pickupRepository;
    private readonly IEventPublisher _eventPublisher;

    public DropoffService(
        IDropoffRepository dropoffRepository,
        IPickupRepository pickupRepository,
        IEventPublisher eventPublisher)
    {
        _dropoffRepository = dropoffRepository;
        _pickupRepository = pickupRepository;
        _eventPublisher = eventPublisher;
    }

    public async Task<(bool Success, int StatusCode, string Message, DropoffRecordDto? Record)> RecordDropoffAsync(
        Guid pickupId, Guid requestingRecyclerId, CreateDropoffRecordRequestDto dto,
        CancellationToken cancellationToken = default)
    {
        if (dto.DropoffDate == null || dto.DropoffDate.Value == default)
        {
            return (false, 400, "Drop-off date is required.", null);
        }

        // One day of slack so a driver in a timezone ahead of UTC (e.g. Sri Lanka, +05:30)
        // can record today's local date shortly after midnight.
        if (dto.DropoffDate.Value.Date > DateTime.UtcNow.Date.AddDays(1))
        {
            return (false, 400, "Drop-off date cannot be in the future.", null);
        }

        TimeSpan? dropoffTime = null;
        if (!string.IsNullOrWhiteSpace(dto.DropoffTime))
        {
            if (!TimeSpan.TryParseExact(dto.DropoffTime.Trim(), @"hh\:mm", CultureInfo.InvariantCulture, out var parsedTime))
            {
                return (false, 400, "Drop-off time must be in HH:mm format.", null);
            }
            dropoffTime = parsedTime;
        }

        var notes = string.IsNullOrWhiteSpace(dto.ConditionNotes) ? null : dto.ConditionNotes.Trim();
        if (notes != null && notes.Length > MaxNotesLength)
        {
            return (false, 400, $"Condition notes must not exceed {MaxNotesLength} characters.", null);
        }

        var pickup = await _pickupRepository.GetByIdAsync(pickupId, cancellationToken);
        if (pickup == null)
        {
            return (false, 404, "Pickup request not found.", null);
        }

        if (pickup.RecyclerId != requestingRecyclerId)
        {
            return (false, 403, "You are not authorized to record a drop-off for this pickup.", null);
        }

        if (pickup.Status == "Collected")
        {
            return (false, 409, "A drop-off has already been recorded for this pickup.", null);
        }

        if (pickup.Status != "Scheduled")
        {
            return (false, 400, "Only a scheduled pickup can be dropped off.", null);
        }

        var record = new DropoffRecord
        {
            Id = Guid.NewGuid(),
            PickupRequestId = pickupId,
            RecordedBy = requestingRecyclerId,
            DropoffDate = dto.DropoffDate.Value.Date,
            DropoffTime = dropoffTime,
            ConditionNotes = notes,
            CreatedAt = DateTime.UtcNow
        };

        var saved = await _dropoffRepository.CreateDropoffAsync(record, cancellationToken);
        if (!saved)
        {
            return (false, 409, "This pickup is no longer awaiting drop-off.", null);
        }

        await _eventPublisher.PublishAsync(
            PickupCollectedEvent.Topic,
            pickupId.ToString(),
            new PickupCollectedEvent
            {
                PickupRequestId = pickupId.ToString(),
                UserId = pickup.UserId.ToString(),
                RecyclerId = requestingRecyclerId.ToString(),
                EstimatedWeightKg = pickup.EstimatedWeightKg,
                ItemCount = pickup.Items.Count
            },
            cancellationToken);

        return (true, 201, "Drop-off recorded. Pickup marked as Collected.", MapToDto(record, "Collected"));
    }

    public async Task<(bool Success, int StatusCode, string Message, DropoffRecordDto? Record)> GetDropoffAsync(
        Guid pickupId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var pickup = await _pickupRepository.GetByIdAsync(pickupId, cancellationToken);
        if (pickup == null)
        {
            return (false, 404, "Pickup request not found.", null);
        }

        var isOwner = pickup.UserId == requestingUserId;
        var isRecycler = pickup.RecyclerId == requestingUserId;
        if (!isOwner && !isRecycler)
        {
            return (false, 403, "You are not authorized to view this drop-off record.", null);
        }

        var record = await _dropoffRepository.GetByPickupRequestIdAsync(pickupId, cancellationToken);
        if (record == null)
        {
            return (false, 404, "No drop-off has been recorded for this pickup.", null);
        }

        return (true, 200, "Drop-off record retrieved.", MapToDto(record, pickup.Status));
    }

    private static DropoffRecordDto MapToDto(DropoffRecord record, string pickupStatus)
    {
        return new DropoffRecordDto
        {
            Id = record.Id,
            PickupRequestId = record.PickupRequestId,
            RecordedBy = record.RecordedBy,
            DropoffDate = record.DropoffDate,
            DropoffTime = record.DropoffTime?.ToString(@"hh\:mm", CultureInfo.InvariantCulture),
            ConditionNotes = record.ConditionNotes,
            CreatedAt = record.CreatedAt,
            PickupStatus = pickupStatus
        };
    }
}
