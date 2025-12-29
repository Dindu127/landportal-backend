using LandPortal.Api.Data;
using LandPortal.Api.Entities;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace LandPortal.Api.Services
{
    public class ContactService : IContactService
    {
        private readonly LandPortalDbContext _db;

        public ContactService(LandPortalDbContext db)
        {
            _db = db;
        }

        // ✔ Check if user already unlocked this property
        public async Task<bool> HasAccessToOwnerContactAsync(Guid propertyId, Guid userId)
        {
            return await _db.ContactViews
                .AnyAsync(x => x.PropertyId == propertyId &&
                               x.UserId == userId &&
                               x.IsPremiumAccess == true);
        }

        // ✔ Add unlock record (if not already unlocked)
        public async Task<ContactView> CreateUnlockAsync(
            Guid propertyId,
            Guid userId,
            decimal? amountPaid,
            string transactionId,
            bool isPremiumAccess)
        {
            // Prevent duplicate unlock entry
            var existing = await _db.ContactViews.FirstOrDefaultAsync(x =>
                x.PropertyId == propertyId &&
                x.UserId == userId &&
                x.IsPremiumAccess == true);

            if (existing != null)
                return existing;

            var entry = new ContactView
            {
                Id = Guid.NewGuid(),
                PropertyId = propertyId,
                UserId = userId,
                AmountPaid = amountPaid,
                TransactionId = transactionId,
                IsPremiumAccess = isPremiumAccess,
                ViewedAt = DateTime.UtcNow
            };

            _db.ContactViews.Add(entry);
            await _db.SaveChangesAsync();

            return entry;
        }
    }
}
