namespace ProfileService.Application.Options;

public sealed class VerificationOptions
{
    public const string SectionName = "Verification";

    public int CodeLength { get; set; } = 6;
    public int ExpirationMinutes { get; set; } = 5;
    public int MaxAttempts { get; set; } = 5;
    public bool ShowCodeInResponse { get; set; } = false;
    public string Provider { get; set; } = "Console";
}
