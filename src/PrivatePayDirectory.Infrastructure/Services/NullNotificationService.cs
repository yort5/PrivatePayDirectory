using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using Microsoft.Extensions.Logging;

namespace PrivatePayDirectory.Infrastructure.Services;

public class NullNotificationService(ILogger<NullNotificationService> logger) : INotificationService
{
    public Task NotifyProfilePendingReviewAsync(Provider provider)
    {
        logger.LogInformation("Notification (not implemented): Profile pending review for {ProviderId}", provider.ProviderId);
        return Task.CompletedTask;
    }

    public Task NotifyProfileApprovedAsync(Provider provider)
    {
        logger.LogInformation("Notification (not implemented): Profile approved for {ProviderId}", provider.ProviderId);
        return Task.CompletedTask;
    }
}
