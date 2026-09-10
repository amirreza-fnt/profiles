using Microsoft.EntityFrameworkCore;
using ProfileService.Application.Interfaces;
using ProfileService.Domain.Entities;

namespace ProfileService.Infrastructure.Persistence;

public sealed class ProfileRepository : IProfileRepository
{
    private readonly ProfileDbContext _db;

    public ProfileRepository(ProfileDbContext db) => _db = db;

    public async Task<UserProfile?> GetByNationalCodeAsync(string nationalCode, CancellationToken ct, bool includeContacts = true)
    {
        IQueryable<UserProfile> q = _db.Profiles;
        if (includeContacts)
            q = q.Include(p => p.Contacts);

        return await q.FirstOrDefaultAsync(p => p.NationalCode == nationalCode, ct);
    }

    public Task AddAsync(UserProfile profile, CancellationToken ct)
    {
        _db.Profiles.Add(profile);
        return Task.CompletedTask;
    }

    public Task AddContactAsync(ContactNumber contact, CancellationToken ct)
    {
        _db.Contacts.Add(contact);
        return Task.CompletedTask;
    }

    public Task AddChallengeAsync(VerificationChallenge challenge, CancellationToken ct)
    {
        _db.VerificationChallenges.Add(challenge);
        return Task.CompletedTask;
    }

    public Task AddAuditAsync(ProfileAuditLog log, CancellationToken ct)
    {
        _db.AuditLogs.Add(log);
        return Task.CompletedTask;
    }

    public void RemoveContact(ContactNumber contact) => _db.Contacts.Remove(contact);

    public Task<ContactNumber?> GetContactAsync(Guid contactId, CancellationToken ct)
        => _db.Contacts.FirstOrDefaultAsync(c => c.Id == contactId, ct);

    public Task<VerificationChallenge?> GetActiveChallengeAsync(Guid contactId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        return _db.VerificationChallenges
            .Where(c => c.ContactId == contactId && c.ConsumedAtUtc == null && c.ExpiresAtUtc >= now)
            .OrderByDescending(c => c.CreatedAtUtc)
            .FirstOrDefaultAsync(ct);
    }

    public async Task InvalidateActiveChallengesAsync(Guid contactId, CancellationToken ct)
    {
        var now = DateTime.UtcNow;
        var active = await _db.VerificationChallenges
            .Where(c => c.ContactId == contactId && c.ConsumedAtUtc == null && c.ExpiresAtUtc >= now)
            .ToListAsync(ct);

        foreach (var c in active)
            c.ConsumedAtUtc = now;
    }

    public Task SaveChangesAsync(CancellationToken ct) => _db.SaveChangesAsync(ct);
}
