namespace ProfileService.Domain.Entities;

/// <summary>
/// پروفایل کاربر. کد ملی شناسه یکتای اصلی است.
/// فیلدهای Sso* فقط از SSO پر می‌شوند؛ فیلدهای User* توسط کاربر/سامانه تکمیل می‌شوند.
/// </summary>
public class UserProfile
{
    /// <summary>کد ملی ۱۰ رقمی — کلید اصلی.</summary>
    public string NationalCode { get; set; } = string.Empty;

    // —— اطلاعات هویتی از SSO ——
    public string? SsoFirstName { get; set; }
    public string? SsoLastName { get; set; }
    public string? SsoFatherName { get; set; }
    public string? SsoGender { get; set; }
    public string? SsoBirthDate { get; set; }
    public string? SsoEmail { get; set; }
    public string? SsoProvince { get; set; }
    public string? SsoCity { get; set; }
    public string? SsoPostalCode { get; set; }
    public DateTime? SsoSyncedAtUtc { get; set; }

    // —— اطلاعات تکمیلی کاربر ——
    public string? UserBirthDate { get; set; }

    /// <summary>FileId از میکروسرویس فایل‌ها (آواتار).</summary>
    public Guid? AvatarFileId { get; set; }
    public string? AvatarShortCode { get; set; }
    public string? AvatarUrl { get; set; }

    /// <summary>FileId از میکروسرویس فایل‌ها (تصویر شخص).</summary>
    public Guid? PhotoFileId { get; set; }
    public string? PhotoShortCode { get; set; }
    public string? PhotoUrl { get; set; }

    public DateTime CreatedAtUtc { get; set; }
    public DateTime UpdatedAtUtc { get; set; }

    public ICollection<ContactNumber> Contacts { get; set; } = new List<ContactNumber>();
    public ICollection<ProfileAuditLog> AuditLogs { get; set; } = new List<ProfileAuditLog>();
}
