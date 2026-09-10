using ProfileService.Application.DTOs;
using ProfileService.Domain.Enums;

namespace ProfileService.Application.Interfaces;

public sealed record CallerContext(
    ActorType ActorType,
    string ActorId,
    string? NationalCode,
    IReadOnlyList<string> Roles,
    string? ServiceName);

public interface IProfileService
{
    Task<CallerContext> ResolveCallerAsync(string? authorizationHeader, string? apiKeyHeader, CancellationToken ct);

    /// <summary>ایجاد/به‌روزرسانی پروفایل پس از لاگین SSO (فقط با ApiKey سرویس).</summary>
    Task<ProfileResponse> EnsureFromSsoAsync(EnsureProfileRequest request, CallerContext caller, CancellationToken ct);

    Task<ProfileResponse> GetByNationalCodeAsync(string nationalCode, CallerContext caller, CancellationToken ct);
    Task<ProfileResponse> GetMeAsync(CallerContext caller, CancellationToken ct);
    Task<ProfileResponse> UpdateMeAsync(UpdateProfileRequest request, CallerContext caller, CancellationToken ct);
    Task<ProfileResponse> SetAvatarAsync(SetFileRefRequest request, CallerContext caller, CancellationToken ct);
    Task<ProfileResponse> SetPhotoAsync(SetFileRefRequest request, CallerContext caller, CancellationToken ct);
    Task<ContactResponse> AddContactAsync(AddContactRequest request, CallerContext caller, CancellationToken ct);
    Task RemoveContactAsync(Guid contactId, CallerContext caller, CancellationToken ct);
    Task<SendVerificationResponse> SendVerificationAsync(Guid contactId, CallerContext caller, CancellationToken ct);
    Task<ContactResponse> ConfirmVerificationAsync(Guid contactId, ConfirmVerificationRequest request, CallerContext caller, CancellationToken ct);
}
