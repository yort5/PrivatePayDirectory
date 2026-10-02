using PrivatePayDirectory.Core.Models;

namespace PrivatePayDirectory.Core.Interfaces;

public interface INotificationService
{
    Task NotifyProfilePendingReviewAsync(Provider provider);
    Task NotifyProfileApprovedAsync(Provider provider);
}
