using ProfileService.Domain.Entities;

namespace ProfileService.Application.Interfaces;

public interface IProfileRepository
{
    Task<UserProfile?> GetByNationalCodeAsync(string nationalCode, CancellationToken ct, bool includeContacts = true);
    Task AddAsync(UserProfile profile, CancellationToken ct);
    Task AddContactAsync(ContactNumber contact, CancellationToken ct);
    Task AddChallengeAsync(VerificationChallenge challenge, CancellationToken ct);
    Task AddAuditAsync(ProfileAuditLog log, CancellationToken ct);
    void RemoveContact(ContactNumber contact);
    Task<ContactNumber?> GetContactAsync(Guid contactId, CancellationToken ct);
    Task<VerificationChallenge?> GetActiveChallengeAsync(Guid contactId, CancellationToken ct);
    Task InvalidateActiveChallengesAsync(Guid contactId, CancellationToken ct);
    Task SaveChangesAsync(CancellationToken ct);
}
