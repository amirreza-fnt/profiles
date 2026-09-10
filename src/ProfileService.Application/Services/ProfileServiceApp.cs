using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using FluentValidation;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using ProfileService.Application.DTOs;
using ProfileService.Application.Exceptions;
using ProfileService.Application.Interfaces;
using ProfileService.Application.Options;
using ProfileService.Domain.Entities;
using ProfileService.Domain.Enums;

namespace ProfileService.Application.Services;

public sealed class ProfileServiceApp : IProfileService
{
    private readonly IProfileRepository _repo;
    private readonly ISsoAuthClient _sso;
    private readonly IVerificationSender _verificationSender;
    private readonly IOptions<InternalAuthOptions> _internalAuth;
    private readonly IOptions<VerificationOptions> _verification;
    private readonly IOptions<FileStorageOptions> _fileStorage;
    private readonly IValidator<EnsureProfileRequest> _ensureValidator;
    private readonly IValidator<UpdateProfileRequest> _updateValidator;
    private readonly IValidator<SetFileRefRequest> _fileValidator;
    private readonly IValidator<AddContactRequest> _contactValidator;
    private readonly IValidator<ConfirmVerificationRequest> _confirmValidator;
    private readonly ILogger<ProfileServiceApp> _logger;

    public ProfileServiceApp(
        IProfileRepository repo,
        ISsoAuthClient sso,
        IVerificationSender verificationSender,
        IOptions<InternalAuthOptions> internalAuth,
        IOptions<VerificationOptions> verification,
        IOptions<FileStorageOptions> fileStorage,
        IValidator<EnsureProfileRequest> ensureValidator,
        IValidator<UpdateProfileRequest> updateValidator,
        IValidator<SetFileRefRequest> fileValidator,
        IValidator<AddContactRequest> contactValidator,
        IValidator<ConfirmVerificationRequest> confirmValidator,
        ILogger<ProfileServiceApp> logger)
    {
        _repo = repo;
        _sso = sso;
        _verificationSender = verificationSender;
        _internalAuth = internalAuth;
        _verification = verification;
        _fileStorage = fileStorage;
        _ensureValidator = ensureValidator;
        _updateValidator = updateValidator;
        _fileValidator = fileValidator;
        _contactValidator = contactValidator;
        _confirmValidator = confirmValidator;
        _logger = logger;
    }

    public async Task<CallerContext> ResolveCallerAsync(string? authorizationHeader, string? apiKeyHeader, CancellationToken ct)
    {
        var opts = _internalAuth.Value;
        if (!string.IsNullOrWhiteSpace(apiKeyHeader))
        {
            var match = opts.ApiKeys.FirstOrDefault(k =>
                !string.IsNullOrWhiteSpace(k.Key) &&
                string.Equals(k.Key, apiKeyHeader.Trim(), StringComparison.Ordinal));

            if (match is not null)
            {
                return new CallerContext(
                    ActorType.Service,
                    match.Name,
                    NationalCode: null,
                    Roles: Array.Empty<string>(),
                    ServiceName: match.Name);
            }

            throw new NotAuthenticatedException("Invalid API key.");
        }

        if (string.IsNullOrWhiteSpace(authorizationHeader))
            throw new NotAuthenticatedException("Authorization header or X-Api-Key is required.");

        var token = authorizationHeader.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase)
            ? authorizationHeader["Bearer ".Length..].Trim()
            : authorizationHeader.Trim();

        if (string.IsNullOrWhiteSpace(token))
            throw new NotAuthenticatedException("Bearer token is required.");

        var user = await _sso.ValidateAsync(token, ct)
            ?? throw new NotAuthenticatedException("Invalid or expired token.");

        if (string.IsNullOrWhiteSpace(user.MelliCode))
            throw new NotAuthenticatedException("Token does not contain national code.");

        return new CallerContext(
            ActorType.User,
            user.MelliCode,
            user.MelliCode,
            user.Roles,
            ServiceName: null);
    }

    public async Task<ProfileResponse> EnsureFromSsoAsync(EnsureProfileRequest request, CallerContext caller, CancellationToken ct)
    {
        RequireService(caller);
        await ValidateAsync(_ensureValidator, request, ct);

        var nationalCode = request.NationalCode.Trim();
        var now = DateTime.UtcNow;
        var profile = await _repo.GetByNationalCodeAsync(nationalCode, ct);
        var created = false;

        if (profile is null)
        {
            profile = new UserProfile
            {
                NationalCode = nationalCode,
                CreatedAtUtc = now,
                UpdatedAtUtc = now
            };
            await _repo.AddAsync(profile, ct);
            created = true;
        }

        profile.SsoFirstName = NullIfWhite(request.FirstName) ?? profile.SsoFirstName;
        profile.SsoLastName = NullIfWhite(request.LastName) ?? profile.SsoLastName;
        profile.SsoFatherName = NullIfWhite(request.FatherName) ?? profile.SsoFatherName;
        profile.SsoGender = NullIfWhite(request.Gender) ?? profile.SsoGender;
        profile.SsoBirthDate = NullIfWhite(request.BirthDate) ?? profile.SsoBirthDate;
        profile.SsoEmail = NullIfWhite(request.Email) ?? profile.SsoEmail;
        profile.SsoProvince = NullIfWhite(request.Province) ?? profile.SsoProvince;
        profile.SsoCity = NullIfWhite(request.City) ?? profile.SsoCity;
        profile.SsoPostalCode = NullIfWhite(request.PostalCode) ?? profile.SsoPostalCode;
        profile.SsoSyncedAtUtc = now;
        profile.UpdatedAtUtc = now;

        if (!string.IsNullOrWhiteSpace(request.Mobile))
        {
            var mobile = request.Mobile.Trim();
            var existing = profile.Contacts.FirstOrDefault(c =>
                c.Type == ContactType.Mobile && c.Number == mobile);

            if (existing is null)
            {
                foreach (var m in profile.Contacts.Where(c => c.Type == ContactType.Mobile && c.IsPrimary))
                    m.IsPrimary = false;

                var contact = new ContactNumber
                {
                    Id = Guid.NewGuid(),
                    NationalCode = nationalCode,
                    Type = ContactType.Mobile,
                    Number = mobile,
                    IsVerified = true,
                    VerifiedAtUtc = now,
                    Source = DataSource.Sso,
                    IsPrimary = true,
                    CreatedAtUtc = now,
                    UpdatedAtUtc = now
                };
                await _repo.AddContactAsync(contact, ct);
                profile.Contacts.Add(contact);
            }
            else if (!existing.IsVerified || existing.Source != DataSource.Sso)
            {
                existing.IsVerified = true;
                existing.VerifiedAtUtc = now;
                existing.Source = DataSource.Sso;
                existing.IsPrimary = true;
                existing.UpdatedAtUtc = now;
            }
        }

        await _repo.AddAuditAsync(new ProfileAuditLog
        {
            Id = Guid.NewGuid(),
            NationalCode = nationalCode,
            Action = created ? AuditAction.ProfileCreated : AuditAction.SsoIdentitySynced,
            ActorType = ActorType.Service,
            ActorId = caller.ActorId,
            Description = JsonSerializer.Serialize(new { source = "SSO", mobileAutoVerified = !string.IsNullOrWhiteSpace(request.Mobile) }),
            CreatedAtUtc = now
        }, ct);

        await _repo.SaveChangesAsync(ct);
        return Map(profile);
    }

    public async Task<ProfileResponse> GetByNationalCodeAsync(string nationalCode, CallerContext caller, CancellationToken ct)
    {
        if (caller.ActorType == ActorType.User)
        {
            if (!string.Equals(caller.NationalCode, nationalCode, StringComparison.Ordinal))
                throw new NotAuthorizedException("You can only access your own profile.");
        }
        else if (caller.ActorType != ActorType.Service)
        {
            throw new NotAuthorizedException("Not authorized.");
        }

        if (!IsValidNationalCode(nationalCode))
            throw new DomainValidationException("National code must be exactly 10 digits.");

        var profile = await _repo.GetByNationalCodeAsync(nationalCode.Trim(), ct)
            ?? throw new NotFoundException("Profile not found.");

        return Map(profile);
    }

    public async Task<ProfileResponse> GetMeAsync(CallerContext caller, CancellationToken ct)
    {
        RequireUser(caller);
        var profile = await _repo.GetByNationalCodeAsync(caller.NationalCode!, ct)
            ?? throw new NotFoundException("Profile not found. Complete SSO login first so the profile can be created.");

        return Map(profile);
    }

    public async Task<ProfileResponse> UpdateMeAsync(UpdateProfileRequest request, CallerContext caller, CancellationToken ct)
    {
        RequireUser(caller);
        await ValidateAsync(_updateValidator, request, ct);

        var profile = await RequireOwnProfileAsync(caller, ct);
        var now = DateTime.UtcNow;

        if (request.UserBirthDate is not null)
            profile.UserBirthDate = NullIfWhite(request.UserBirthDate);

        profile.UpdatedAtUtc = now;

        await _repo.AddAuditAsync(new ProfileAuditLog
        {
            Id = Guid.NewGuid(),
            NationalCode = profile.NationalCode,
            Action = AuditAction.UserProfileUpdated,
            ActorType = ActorType.User,
            ActorId = caller.NationalCode,
            Description = JsonSerializer.Serialize(new { userBirthDate = profile.UserBirthDate }),
            CreatedAtUtc = now
        }, ct);

        await _repo.SaveChangesAsync(ct);
        return Map(profile);
    }

    public async Task<ProfileResponse> SetAvatarAsync(SetFileRefRequest request, CallerContext caller, CancellationToken ct)
    {
        RequireUser(caller);
        await ValidateAsync(_fileValidator, request, ct);

        var profile = await RequireOwnProfileAsync(caller, ct);
        var now = DateTime.UtcNow;
        var previous = profile.AvatarFileId;

        profile.AvatarFileId = request.FileId;
        profile.AvatarShortCode = request.ShortCode.Trim();
        profile.AvatarUrl = ResolveFileUrl(request.Url, request.ShortCode);
        profile.UpdatedAtUtc = now;

        await _repo.AddAuditAsync(new ProfileAuditLog
        {
            Id = Guid.NewGuid(),
            NationalCode = profile.NationalCode,
            Action = AuditAction.AvatarChanged,
            ActorType = ActorType.User,
            ActorId = caller.NationalCode,
            Description = JsonSerializer.Serialize(new { previous, fileId = request.FileId, shortCode = request.ShortCode }),
            CreatedAtUtc = now
        }, ct);

        await _repo.SaveChangesAsync(ct);
        return Map(profile);
    }

    public async Task<ProfileResponse> SetPhotoAsync(SetFileRefRequest request, CallerContext caller, CancellationToken ct)
    {
        RequireUser(caller);
        await ValidateAsync(_fileValidator, request, ct);

        var profile = await RequireOwnProfileAsync(caller, ct);
        var now = DateTime.UtcNow;
        var previous = profile.PhotoFileId;

        profile.PhotoFileId = request.FileId;
        profile.PhotoShortCode = request.ShortCode.Trim();
        profile.PhotoUrl = ResolveFileUrl(request.Url, request.ShortCode);
        profile.UpdatedAtUtc = now;

        await _repo.AddAuditAsync(new ProfileAuditLog
        {
            Id = Guid.NewGuid(),
            NationalCode = profile.NationalCode,
            Action = AuditAction.PhotoChanged,
            ActorType = ActorType.User,
            ActorId = caller.NationalCode,
            Description = JsonSerializer.Serialize(new { previous, fileId = request.FileId, shortCode = request.ShortCode }),
            CreatedAtUtc = now
        }, ct);

        await _repo.SaveChangesAsync(ct);
        return Map(profile);
    }

    public async Task<ContactResponse> AddContactAsync(AddContactRequest request, CallerContext caller, CancellationToken ct)
    {
        RequireUser(caller);
        await ValidateAsync(_contactValidator, request, ct);

        var profile = await RequireOwnProfileAsync(caller, ct);
        var number = request.Number.Trim();
        var now = DateTime.UtcNow;

        if (profile.Contacts.Any(c => c.Number == number && c.Type == request.Type))
            throw new ConflictException("This contact number already exists on the profile.");

        if (request.IsPrimary)
        {
            foreach (var c in profile.Contacts.Where(c => c.Type == request.Type && c.IsPrimary))
                c.IsPrimary = false;
        }

        var contact = new ContactNumber
        {
            Id = Guid.NewGuid(),
            NationalCode = profile.NationalCode,
            Type = request.Type,
            Number = number,
            IsVerified = false,
            Source = DataSource.User,
            IsPrimary = request.IsPrimary,
            CreatedAtUtc = now,
            UpdatedAtUtc = now
        };

        await _repo.AddContactAsync(contact, ct);

        await _repo.AddAuditAsync(new ProfileAuditLog
        {
            Id = Guid.NewGuid(),
            NationalCode = profile.NationalCode,
            Action = request.Type == ContactType.Mobile ? AuditAction.MobileAdded : AuditAction.LandlineAdded,
            ActorType = ActorType.User,
            ActorId = caller.NationalCode,
            Description = JsonSerializer.Serialize(new { contactId = contact.Id, type = request.Type.ToString(), number = MaskPhone(number) }),
            CreatedAtUtc = now
        }, ct);

        profile.UpdatedAtUtc = now;
        await _repo.SaveChangesAsync(ct);

        return MapContact(contact);
    }

    public async Task RemoveContactAsync(Guid contactId, CallerContext caller, CancellationToken ct)
    {
        RequireUser(caller);
        var profile = await RequireOwnProfileAsync(caller, ct);
        var contact = profile.Contacts.FirstOrDefault(c => c.Id == contactId)
            ?? throw new NotFoundException("Contact not found.");

        if (contact.Source == DataSource.Sso && contact.Type == ContactType.Mobile && contact.IsPrimary)
            throw new DomainValidationException("Primary SSO mobile cannot be removed.");

        _repo.RemoveContact(contact);
        profile.UpdatedAtUtc = DateTime.UtcNow;

        await _repo.AddAuditAsync(new ProfileAuditLog
        {
            Id = Guid.NewGuid(),
            NationalCode = profile.NationalCode,
            Action = AuditAction.ContactRemoved,
            ActorType = ActorType.User,
            ActorId = caller.NationalCode,
            Description = JsonSerializer.Serialize(new { contactId, type = contact.Type.ToString(), number = MaskPhone(contact.Number) }),
            CreatedAtUtc = DateTime.UtcNow
        }, ct);

        await _repo.SaveChangesAsync(ct);
    }

    public async Task<SendVerificationResponse> SendVerificationAsync(Guid contactId, CallerContext caller, CancellationToken ct)
    {
        RequireUser(caller);
        var profile = await RequireOwnProfileAsync(caller, ct);
        var contact = profile.Contacts.FirstOrDefault(c => c.Id == contactId)
            ?? throw new NotFoundException("Contact not found.");

        if (contact.IsVerified)
            throw new DomainValidationException("Contact is already verified.");

        var opts = _verification.Value;
        var channel = contact.Type == ContactType.Landline ? VerificationChannel.Call : VerificationChannel.Sms;
        var code = GenerateNumericCode(opts.CodeLength);
        var now = DateTime.UtcNow;

        await _repo.InvalidateActiveChallengesAsync(contactId, ct);

        var challenge = new VerificationChallenge
        {
            Id = Guid.NewGuid(),
            ContactId = contactId,
            CodeHash = HashCode(code),
            Channel = channel,
            ExpiresAtUtc = now.AddMinutes(opts.ExpirationMinutes),
            Attempts = 0,
            MaxAttempts = opts.MaxAttempts,
            CreatedAtUtc = now
        };

        await _repo.AddChallengeAsync(challenge, ct);

        await _verificationSender.SendAsync(contact.Number, code, channel, ct);

        await _repo.AddAuditAsync(new ProfileAuditLog
        {
            Id = Guid.NewGuid(),
            NationalCode = profile.NationalCode,
            Action = AuditAction.VerificationSent,
            ActorType = ActorType.User,
            ActorId = caller.NationalCode,
            Description = JsonSerializer.Serialize(new { contactId, channel = channel.ToString(), number = MaskPhone(contact.Number) }),
            CreatedAtUtc = now
        }, ct);

        await _repo.SaveChangesAsync(ct);

        _logger.LogInformation("Verification sent via {Channel} for contact {ContactId}", channel, contactId);

        return new SendVerificationResponse
        {
            ContactId = contactId,
            Channel = channel,
            ExpiresInSeconds = opts.ExpirationMinutes * 60,
            DebugCode = opts.ShowCodeInResponse ? code : null
        };
    }

    public async Task<ContactResponse> ConfirmVerificationAsync(Guid contactId, ConfirmVerificationRequest request, CallerContext caller, CancellationToken ct)
    {
        RequireUser(caller);
        await ValidateAsync(_confirmValidator, request, ct);

        var profile = await RequireOwnProfileAsync(caller, ct);
        var contact = profile.Contacts.FirstOrDefault(c => c.Id == contactId)
            ?? throw new NotFoundException("Contact not found.");

        if (contact.IsVerified)
            return MapContact(contact);

        var challenge = await _repo.GetActiveChallengeAsync(contactId, ct)
            ?? throw new DomainValidationException("No active verification challenge. Request a new code.");

        if (DateTime.UtcNow > challenge.ExpiresAtUtc)
            throw new DomainValidationException("Verification code expired.");

        if (challenge.Attempts >= challenge.MaxAttempts)
            throw new DomainValidationException("Maximum verification attempts exceeded.");

        challenge.Attempts++;

        if (!FixedTimeEquals(challenge.CodeHash, HashCode(request.Code.Trim())))
        {
            await _repo.SaveChangesAsync(ct);
            throw new DomainValidationException("Invalid verification code.");
        }

        var now = DateTime.UtcNow;
        challenge.ConsumedAtUtc = now;
        contact.IsVerified = true;
        contact.VerifiedAtUtc = now;
        contact.UpdatedAtUtc = now;
        profile.UpdatedAtUtc = now;

        await _repo.AddAuditAsync(new ProfileAuditLog
        {
            Id = Guid.NewGuid(),
            NationalCode = profile.NationalCode,
            Action = contact.Type == ContactType.Mobile ? AuditAction.MobileVerified : AuditAction.LandlineVerified,
            ActorType = ActorType.User,
            ActorId = caller.NationalCode,
            Description = JsonSerializer.Serialize(new { contactId, type = contact.Type.ToString(), number = MaskPhone(contact.Number) }),
            CreatedAtUtc = now
        }, ct);

        await _repo.SaveChangesAsync(ct);
        return MapContact(contact);
    }

    private async Task<UserProfile> RequireOwnProfileAsync(CallerContext caller, CancellationToken ct)
    {
        return await _repo.GetByNationalCodeAsync(caller.NationalCode!, ct)
            ?? throw new NotFoundException("Profile not found. Complete SSO login first so the profile can be created.");
    }

    private static void RequireUser(CallerContext caller)
    {
        if (caller.ActorType != ActorType.User || string.IsNullOrWhiteSpace(caller.NationalCode))
            throw new NotAuthorizedException("A valid user token is required for this operation.");
    }

    private static void RequireService(CallerContext caller)
    {
        if (caller.ActorType != ActorType.Service)
            throw new NotAuthorizedException("A valid service API key is required for this operation.");
    }

    private string? ResolveFileUrl(string? url, string shortCode)
    {
        if (!string.IsNullOrWhiteSpace(url))
            return url.Trim();

        var baseUrl = _fileStorage.Value.BaseUrl?.TrimEnd('/');
        if (string.IsNullOrWhiteSpace(baseUrl))
            return $"/i/{shortCode.Trim()}";

        return $"{baseUrl}/i/{shortCode.Trim()}";
    }

    private ProfileResponse Map(UserProfile profile)
    {
        return new ProfileResponse
        {
            NationalCode = profile.NationalCode,
            Identity = new IdentityBlockDto
            {
                Source = DataSource.Sso,
                FirstName = profile.SsoFirstName,
                LastName = profile.SsoLastName,
                FatherName = profile.SsoFatherName,
                Gender = profile.SsoGender,
                BirthDate = profile.SsoBirthDate,
                Email = profile.SsoEmail,
                Province = profile.SsoProvince,
                City = profile.SsoCity,
                PostalCode = profile.SsoPostalCode,
                SyncedAtUtc = profile.SsoSyncedAtUtc
            },
            UserCompleted = new UserCompletedBlockDto
            {
                Source = DataSource.User,
                BirthDate = profile.UserBirthDate,
                Avatar = profile.AvatarFileId is Guid aid
                    ? new FileRefDto { FileId = aid, ShortCode = profile.AvatarShortCode ?? "", Url = profile.AvatarUrl }
                    : null,
                Photo = profile.PhotoFileId is Guid pid
                    ? new FileRefDto { FileId = pid, ShortCode = profile.PhotoShortCode ?? "", Url = profile.PhotoUrl }
                    : null
            },
            Contacts = profile.Contacts
                .OrderByDescending(c => c.IsPrimary)
                .ThenBy(c => c.Type)
                .Select(MapContact)
                .ToList(),
            CreatedAtUtc = profile.CreatedAtUtc,
            UpdatedAtUtc = profile.UpdatedAtUtc
        };
    }

    private static ContactResponse MapContact(ContactNumber c) => new()
    {
        Id = c.Id,
        Type = c.Type,
        Number = c.Number,
        IsVerified = c.IsVerified,
        VerifiedAtUtc = c.VerifiedAtUtc,
        Source = c.Source,
        IsPrimary = c.IsPrimary
    };

    private static async Task ValidateAsync<T>(IValidator<T> validator, T instance, CancellationToken ct)
    {
        var result = await validator.ValidateAsync(instance, ct);
        if (!result.IsValid)
            throw new DomainValidationException(result.Errors[0].ErrorMessage);
    }

    private static bool IsValidNationalCode(string? value)
        => !string.IsNullOrWhiteSpace(value) && System.Text.RegularExpressions.Regex.IsMatch(value.Trim(), @"^\d{10}$");

    private static string? NullIfWhite(string? value)
        => string.IsNullOrWhiteSpace(value) ? null : value.Trim();

    private static string GenerateNumericCode(int length)
    {
        length = Math.Clamp(length, 4, 8);
        var max = (int)Math.Pow(10, length);
        var value = RandomNumberGenerator.GetInt32(0, max);
        return value.ToString($"D{length}");
    }

    private static string HashCode(string code)
    {
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(code));
        return Convert.ToHexString(bytes);
    }

    private static bool FixedTimeEquals(string a, string b)
    {
        var ba = Encoding.UTF8.GetBytes(a);
        var bb = Encoding.UTF8.GetBytes(b);
        return ba.Length == bb.Length && CryptographicOperations.FixedTimeEquals(ba, bb);
    }

    private static string MaskPhone(string number)
    {
        if (number.Length < 4) return "****";
        return number[..3] + new string('*', Math.Max(0, number.Length - 5)) + number[^2..];
    }
}
