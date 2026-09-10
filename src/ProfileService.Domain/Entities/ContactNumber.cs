using ProfileService.Domain.Enums;

namespace ProfileService.Domain.Entities;

/// <summary>
/// شماره تماس (موبایل یا تلفن ثابت). استفاده بدون IsVerified مجاز نیست.
/// </summary>
public class ContactNumber
{
    public Guid Id { get; set; }

    public string NationalCode { get; set; } = string.Empty;

    public ContactType Type { get; set; }

    public string Number { get; set; } = string.Empty;

    public bool IsVerified { get; set; }

    public DateTime? VerifiedAtUtc { get; set; }

    public DataSource Source { get; set; }

    public bool IsPrimary { get; set; }

    public DateTime CreatedAtUtc { get; set; }

    public DateTime UpdatedAtUtc { get; set; }

    public UserProfile? Profile { get; set; }

    public ICollection<VerificationChallenge> Challenges { get; set; } = new List<VerificationChallenge>();
}
