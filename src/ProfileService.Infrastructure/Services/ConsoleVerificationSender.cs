using Microsoft.Extensions.Logging;
using ProfileService.Application.Interfaces;
using ProfileService.Domain.Enums;

namespace ProfileService.Infrastructure.Services;

/// <summary>
/// ارسال‌کننده Verification برای توسعه و استقرار آفلاین.
/// در production می‌توان با ارائه‌دهنده SMS/تماس جایگزین کرد.
/// </summary>
public sealed class ConsoleVerificationSender : IVerificationSender
{
    private readonly ILogger<ConsoleVerificationSender> _logger;

    public ConsoleVerificationSender(ILogger<ConsoleVerificationSender> logger) => _logger = logger;

    public Task SendAsync(string destination, string code, VerificationChannel channel, CancellationToken ct)
    {
        _logger.LogWarning(
            "VERIFICATION [{Channel}] to {Destination}: code={Code}",
            channel, destination, code);
        return Task.CompletedTask;
    }
}
