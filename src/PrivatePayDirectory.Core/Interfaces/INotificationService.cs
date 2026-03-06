using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Core.Interfaces;

public interface INotificationService
{
    Task NotifyProfilePendingReviewAsync(Therapist therapist);
    Task NotifyProfileApprovedAsync(Therapist therapist);
}
