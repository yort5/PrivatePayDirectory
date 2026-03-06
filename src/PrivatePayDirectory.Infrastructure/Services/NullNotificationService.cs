using PrivatePayDirectory.Core.Interfaces;
using PrivatePayDirectory.Core.Models;
using Microsoft.Extensions.Logging;

namespace PrivatePayDirectory.Infrastructure.Services;

public class NullNotificationService(ILogger<NullNotificationService> logger) : INotificationService
{
    public Task NotifyProfilePendingReviewAsync(Therapist therapist)
    {
        logger.LogInformation("Notification (not implemented): Profile pending review for {TherapistId}", therapist.TherapistId);
        return Task.CompletedTask;
    }

    public Task NotifyProfileApprovedAsync(Therapist therapist)
    {
        logger.LogInformation("Notification (not implemented): Profile approved for {TherapistId}", therapist.TherapistId);
        return Task.CompletedTask;
    }
}
