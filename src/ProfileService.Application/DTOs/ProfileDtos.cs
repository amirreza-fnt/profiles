using ProfileService.Domain.Enums;

namespace ProfileService.Application.DTOs;

public sealed class EnsureProfileRequest
{
    public string NationalCode { get; set; } = string.Empty;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FatherName { get; set; }
    public string? Gender { get; set; }
    public string? BirthDate { get; set; }
    public string? Email { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    /// <summary>موبایل دریافتی از SSO — به‌صورت خودکار Verified ثبت می‌شود.</summary>
    public string? Mobile { get; set; }
}

public sealed class UpdateProfileRequest
{
    public string? UserBirthDate { get; set; }
}

public sealed class SetFileRefRequest
{
    public Guid FileId { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string? Url { get; set; }
}

public sealed class AddContactRequest
{
    public ContactType Type { get; set; }
    public string Number { get; set; } = string.Empty;
    public bool IsPrimary { get; set; }
}

public sealed class ConfirmVerificationRequest
{
    public string Code { get; set; } = string.Empty;
}

public sealed class SendVerificationResponse
{
    public Guid ContactId { get; set; }
    public VerificationChannel Channel { get; set; }
    public int ExpiresInSeconds { get; set; }
    /// <summary>فقط در محیط توسعه وقتی ShowCodeInResponse=true.</summary>
    public string? DebugCode { get; set; }
}

public sealed class ContactResponse
{
    public Guid Id { get; set; }
    public ContactType Type { get; set; }
    public string Number { get; set; } = string.Empty;
    public bool IsVerified { get; set; }
    public DateTime? VerifiedAtUtc { get; set; }
    public DataSource Source { get; set; }
    public bool IsPrimary { get; set; }
}

public sealed class IdentityBlockDto
{
    public DataSource Source { get; set; } = DataSource.Sso;
    public string? FirstName { get; set; }
    public string? LastName { get; set; }
    public string? FatherName { get; set; }
    public string? Gender { get; set; }
    public string? BirthDate { get; set; }
    public string? Email { get; set; }
    public string? Province { get; set; }
    public string? City { get; set; }
    public string? PostalCode { get; set; }
    public DateTime? SyncedAtUtc { get; set; }
}

public sealed class UserCompletedBlockDto
{
    public DataSource Source { get; set; } = DataSource.User;
    public string? BirthDate { get; set; }
    public FileRefDto? Avatar { get; set; }
    public FileRefDto? Photo { get; set; }
}

public sealed class FileRefDto
{
    public Guid FileId { get; set; }
    public string ShortCode { get; set; } = string.Empty;
    public string? Url { get; set; }
}

public sealed class ProfileResponse
{
    public string NationalCode { get; set; } = string.Empty;
    public IdentityBlockDto Identity { get; set; } = new();
    public UserCompletedBlockDto UserCompleted { get; set; } = new();
    public List<ContactResponse> Contacts { get; set; } = new();
    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }
}
