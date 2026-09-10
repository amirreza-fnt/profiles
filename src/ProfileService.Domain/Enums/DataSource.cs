namespace ProfileService.Domain.Enums;

/// <summary>منبع داده برای تفکیک اطلاعات هویتی SSO از اطلاعات تکمیل‌شده توسط کاربر.</summary>
public enum DataSource
{
    Sso = 1,
    User = 2,
    System = 3
}
