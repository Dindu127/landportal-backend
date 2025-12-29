using LandPortal.Api.Entities;

namespace LandPortal.Api.Services
{
    public interface IUnlockLogService
    {
        Task<ContactUnlockLog> CreateAsync(ContactUnlockLog log);
        Task<ContactUnlockLog?> FindByPaymentIdAsync(string paymentId);

        // Pending unlocks

        Task<PendingUnlock> CreatePendingAsync(PendingUnlock pending);
        Task<PendingUnlock?> FindPendingByOrderIdAsync(string orderId);
        Task MarkPendingCompletedAsync(PendingUnlock pending, Guid unlockLogId);
        Task InsertViaProcAsync(ContactUnlockLog log);

        // optional: get pendings for a user
        Task<IEnumerable<PendingUnlock>> GetPendingForUserAsync(Guid userId);

    }
}
