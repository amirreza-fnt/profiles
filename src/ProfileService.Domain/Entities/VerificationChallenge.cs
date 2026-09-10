using ProfileService.Domain.Enums;

namespace ProfileService.Domain.Entities;

/// <summary>کد Verification کوتاه‌مدت، یکبارمصرف با محدودیت تلاش.</summary>
public class VerificationChallenge
{
    public Guid Id { get; set; }

    public Guid ContactId { get; set; }

    public string CodeHash { get; set; } = string.Empty;

    public VerificationChannel Channel { get; set; }

    public DateTime ExpiresAtUtc { get; set; }

    public int Attempts { get; set; }

    public int MaxAttempts { get; set; }

    public DateTime? ConsumedAtUtc { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public ContactNumber? Contact { get; set; }

    public bool IsActive => ConsumedAtUtc is null && DateTime.UtcNow <= ExpiresAtUtc && Attempts < MaxAttempts;
}
