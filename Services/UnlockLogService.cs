using System;
using System.Collections.Generic;
using System.Data;
using System.Linq;
using System.Threading.Tasks;
using LandPortal.Api.Data;
using LandPortal.Api.Entities;
using Microsoft.EntityFrameworkCore;

namespace LandPortal.Api.Services
{
    public class UnlockLogService : IUnlockLogService
    {
        private readonly LandPortalDbContext _db;
        public UnlockLogService(LandPortalDbContext db) => _db = db;

        public async Task<ContactUnlockLog> CreateAsync(ContactUnlockLog log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));
            log.Id = log.Id == Guid.Empty ? Guid.NewGuid() : log.Id;
            log.CreatedAt = DateTime.UtcNow;
            await _db.ContactUnlockLogs.AddAsync(log);
            await _db.SaveChangesAsync();
            return log;
        }

        /// <summary>
        /// Call stored procedure dbo.Sp_InsertContactUnlockLog using the current DbConnection.
        /// Note: Do not dispose the connection returned by the DbContext.
        /// </summary>
        public async Task InsertViaProcAsync(ContactUnlockLog log)
        {
            if (log == null) throw new ArgumentNullException(nameof(log));

            var conn = _db.Database.GetDbConnection();

            // Ensure connection is open (don't dispose it - EF owns it)
            var shouldClose = false;
            if (conn.State != ConnectionState.Open)
            {
                await conn.OpenAsync();
                shouldClose = true;
            }

            try
            {
                await using var cmd = conn.CreateCommand();
                cmd.CommandType = CommandType.StoredProcedure;
                cmd.CommandText = "dbo.Sp_InsertContactUnlockLog";
                // optional: cmd.CommandTimeout = 60;

                //cmd.Parameters.Add(new SqlParameter("@PropertyId", SqlDbType.UniqueIdentifier) { Value = log.PropertyId });
                //cmd.Parameters.Add(new SqlParameter("@PropertyTitle", SqlDbType.NVarChar, 250) { Value = (object?)log.PropertyTitle ?? DBNull.Value });
                //cmd.Parameters.Add(new SqlParameter("@UnlockedByUserId", SqlDbType.UniqueIdentifier) { Value = log.UnlockedByUserId });
                //cmd.Parameters.Add(new SqlParameter("@UnlockedByUserEmail", SqlDbType.NVarChar, 200) { Value = (object?)log.UnlockedByUserEmail ?? DBNull.Value });
                //cmd.Parameters.Add(new SqlParameter("@UnlockedByUserName", SqlDbType.NVarChar, 200) { Value = (object?)log.UnlockedByUserName ?? DBNull.Value });

                //cmd.Parameters.Add(new SqlParameter("@PaymentId", SqlDbType.NVarChar, 200) { Value = (object?)log.PaymentId ?? DBNull.Value });

                //var pAmt = new SqlParameter("@PaymentAmount", SqlDbType.Decimal)
                //{
                //    Precision = 18,
                //    Scale = 2,
                //    Value = (object?)log.PaymentAmount ?? DBNull.Value
                //};
                //cmd.Parameters.Add(pAmt);

                //cmd.Parameters.Add(new SqlParameter("@Currency", SqlDbType.NVarChar, 10) { Value = (object?)log.Currency ?? DBNull.Value });
                //cmd.Parameters.Add(new SqlParameter("@PaymentStatus", SqlDbType.NVarChar, 50) { Value = (object?)log.PaymentStatus ?? DBNull.Value });
                //cmd.Parameters.Add(new SqlParameter("@Notes", SqlDbType.NVarChar, 1000) { Value = (object?)log.Notes ?? DBNull.Value });

                await cmd.ExecuteNonQueryAsync();
            }
            finally
            {
                if (shouldClose && conn.State == ConnectionState.Open)
                {
                    await conn.CloseAsync();
                }
            }
        }

        public async Task<ContactUnlockLog?> FindByPaymentIdAsync(string paymentId)
        {
            if (string.IsNullOrEmpty(paymentId)) return null;
            return await _db.ContactUnlockLogs.FirstOrDefaultAsync(x => x.PaymentId == paymentId);
        }

        public async Task<PendingUnlock> CreatePendingAsync(PendingUnlock pending)
        {
            if (pending == null) throw new ArgumentNullException(nameof(pending));
            pending.Id = pending.Id == Guid.Empty ? Guid.NewGuid() : pending.Id;
            pending.CreatedAt = DateTime.UtcNow;
            pending.Status = "Pending";

            await _db.PendingUnlocks.AddAsync(pending);
            await _db.SaveChangesAsync();
            return pending;
        }

        public async Task<PendingUnlock?> FindPendingByOrderIdAsync(string orderId)
        {
            if (string.IsNullOrEmpty(orderId)) return null;
            return await _db.PendingUnlocks.FirstOrDefaultAsync(x => x.OrderId == orderId);
        }

        public async Task MarkPendingCompletedAsync(PendingUnlock pending, Guid unlockLogId)
        {
            if (pending == null) throw new ArgumentNullException(nameof(pending));
            pending.Status = "Completed";
            pending.CompletedAt = DateTime.UtcNow;
            pending.UnlockLogId = unlockLogId;
            _db.PendingUnlocks.Update(pending);
            await _db.SaveChangesAsync();
        }

        public async Task<IEnumerable<PendingUnlock>> GetPendingForUserAsync(Guid userId)
        {
            return await _db.PendingUnlocks.Where(x => x.UserId == userId).OrderByDescending(x => x.CreatedAt).ToListAsync();
        }
    }
}
