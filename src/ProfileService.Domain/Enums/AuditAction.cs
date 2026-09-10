namespace ProfileService.Domain.Enums;

public enum AuditAction
{
    ProfileCreated = 1,
    SsoIdentitySynced = 2,
    UserProfileUpdated = 3,
    MobileAdded = 4,
    MobileChanged = 5,
    MobileVerified = 6,
    LandlineAdded = 7,
    LandlineChanged = 8,
    LandlineVerified = 9,
    ContactRemoved = 10,
    AvatarChanged = 11,
    PhotoChanged = 12,
    VerificationSent = 13
}
