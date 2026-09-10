namespace ProfileService.Application.Options;

public sealed class SsoOptions
{
    public const string SectionName = "Sso";

    public string BaseUrl { get; set; } = "http://127.0.0.1:5001";
    public string UserInfoPath { get; set; } = "/api/auth/me";
    public int TimeoutSeconds { get; set; } = 8;
    public int RetryCount { get; set; } = 2;
    public int CircuitBreakerMinThroughput { get; set; } = 5;
    public double CircuitBreakerFailureRatio { get; set; } = 50;
}
