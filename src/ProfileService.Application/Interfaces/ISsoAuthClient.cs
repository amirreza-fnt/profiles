namespace ProfileService.Application.Interfaces;

public sealed record SsoUserInfo(
    string Id,
    string? MelliCode,
    string? Phone,
    IReadOnlyList<string> Roles,
    string? GroupId);

public interface ISsoAuthClient
{
    Task<SsoUserInfo?> ValidateAsync(string bearerToken, CancellationToken ct);
}
