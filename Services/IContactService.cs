using System;
using System.Threading.Tasks;
using LandPortal.Api.Entities;

namespace LandPortal.Api.Services
{
    public interface IContactService
    {
        Task<bool> HasAccessToOwnerContactAsync(Guid propertyId, Guid userId);

        Task<ContactView> CreateUnlockAsync(
            Guid propertyId,
            Guid userId,
            decimal? amountPaid,
            string transactionId,
            bool isPremiumAccess);
    }
}
