using EcoTrack.LogisticsService.Data;
using EcoTrack.LogisticsService.DTOs;
using EcoTrack.LogisticsService.Models;
using EcoTrack.LogisticsService.Repositories;
using EcoTrack.LogisticsService.Events;
using EcoTrack.LogisticsService.Messaging;

namespace EcoTrack.LogisticsService.Services;

public class DisposalCertificateService : IDisposalCertificateService
{
       private readonly IDisposalCertificateRepository _certRepository;
    private readonly IPickupRepository _pickupRepository;
    private readonly IDbConnectionFactory _connectionFactory;
    private readonly IEventPublisher _eventPublisher;

    public DisposalCertificateService(
        IDisposalCertificateRepository certRepository,
        IPickupRepository pickupRepository,
        IDbConnectionFactory connectionFactory,
        IEventPublisher eventPublisher)
    {
        _certRepository = certRepository;
        _pickupRepository = pickupRepository;
        _connectionFactory = connectionFactory;
        _eventPublisher = eventPublisher;
    }

    public async Task<(bool Success, int StatusCode, string Message, DisposalCertificateDto? Certificate)> CreateCertificateAsync(
        Guid recyclerId, string recyclerName, Guid pickupItemId,
        string disposalMethod, CancellationToken cancellationToken = default)
    {
        // Validate disposal method
        if (string.IsNullOrWhiteSpace(disposalMethod))
        {
            return (false, 400, "Disposal method is required.", null);
        }

        // Check the item exists and is assigned to this recycler
        var recyclerPickups = await _pickupRepository.GetByRecyclerIdAsync(recyclerId, cancellationToken);
        var item = recyclerPickups.SelectMany(p => p.Items).FirstOrDefault(i => i.Id == pickupItemId);

        if (item == null)
        {
            return (false, 404, "Pickup item not found or not assigned to you.", null);
        }

        // Check if item is already certified as disposed
        var existingCert = await _certRepository.GetByPickupItemIdAsync(pickupItemId, cancellationToken);
        if (existingCert != null)
        {
            return (false, 409, "This item has already been certified as disposed.", null);
        }

        var result = await _certRepository.CreateCertificateAsync(
            pickupItemId, recyclerId, recyclerName, disposalMethod, cancellationToken);

        if (!result.Success)
        {
            return (false, 404, "Pickup item not found or not assigned to you.", null);
        }

                await _eventPublisher.PublishAsync(
            EwasteDisposalCertifiedEvent.Topic,
            pickupItemId.ToString(),
            new EwasteDisposalCertifiedEvent
            {
                CertificateId = result.Certificate?.Id.ToString() ?? string.Empty,
                PickupRequestId = item.PickupRequestId.ToString(),
                RecyclerId = recyclerId.ToString(),
                WeightKg = item.EstimatedWeightKg,
                DisposalMethod = disposalMethod,
                CertificateNumber = result.Certificate?.Id.ToString() ?? string.Empty
            },
            cancellationToken);

        return (true, 201, "Disposal certificate created.", result.Certificate);
    }

    public async Task<(bool Success, int StatusCode, string Message, ItemWithCertificationDto? Item)> GetItemWithCertificateAsync(
        Guid pickupItemId, Guid requestingUserId, CancellationToken cancellationToken = default)
    {
        var cert = await _certRepository.GetByPickupItemIdAsync(pickupItemId, cancellationToken);

        if (cert == null)
        {
            return (false, 404, "No disposal certificate found for this item.", null);
        }

        // The cert includes UserId (owner) from the JOIN with PickupRequests
        var isOwner = cert.UserId == requestingUserId;
        var isRecycler = cert.RecyclerId == requestingUserId;

        if (!isOwner && !isRecycler)
        {
            return (false, 403, "You are not authorized to view this certification.", null);
        }

        var itemDto = new ItemWithCertificationDto
        {
            Id = cert.PickupItemId,
            ItemName = cert.ItemName,
            Quantity = cert.Quantity,
            ItemCondition = cert.ItemCondition,
            Status = "Disposed",
            Certificate = cert
        };

        return (true, 200, "Certification retrieved.", itemDto);
    }
}
