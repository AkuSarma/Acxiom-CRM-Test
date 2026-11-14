namespace AcxiomCRM.Services;

public class PasswordHistoryService(AppDb db, IPasswordHasher<AppUser> hasher) {
    const int HistoryLimit = 5;

    public async Task<bool> IsPasswordReusedAsync(AppUser user, string newPlainPassword) {
        if (!string.IsNullOrEmpty(user.PasswordHash) &&
            hasher.VerifyHashedPassword(user, user.PasswordHash, newPlainPassword) != PasswordVerificationResult.Failed) {
            return true;
        }
        var recent = await db.PasswordHistories
            .Where(p => p.UserId == user.Id)
            .OrderByDescending(p => p.CreatedDate)
            .Take(HistoryLimit)
            .ToListAsync();

        foreach (var entry in recent) {
            if (hasher.VerifyHashedPassword(user, entry.PasswordHash, newPlainPassword) != PasswordVerificationResult.Failed) {
                return true;
            }
        }
        return false;
    }

    public async Task RecordPasswordAsync(AppUser user) {
        if (string.IsNullOrEmpty(user.PasswordHash)) return;
        db.PasswordHistories.Add(new PasswordHistory {
            UserId = user.Id,
            PasswordHash = user.PasswordHash,
            CreatedDate = DateTime.UtcNow
        });
        await db.SaveChangesAsync();
    }
}
