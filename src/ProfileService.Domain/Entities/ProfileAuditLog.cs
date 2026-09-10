using ProfileService.Domain.Enums;

namespace ProfileService.Domain.Entities;

/// <summary>لاگ تغییرات مهم پروفایل (append-only).</summary>
public class ProfileAuditLog
{
    public Guid Id { get; set; }

    public string NationalCode { get; set; } = string.Empty;

    public AuditAction Action { get; set; }

    public ActorType ActorType { get; set; }

    /// <summary>کد ملی کاربر / نام سرویس / null برای System.</summary>
    public string? ActorId { get; set; }

    public string? Description { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public UserProfile? Profile { get; set; }
}
