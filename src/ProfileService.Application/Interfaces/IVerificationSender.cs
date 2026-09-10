using ProfileService.Domain.Enums;

namespace ProfileService.Application.Interfaces;

public interface IVerificationSender
{
    Task SendAsync(string destination, string code, VerificationChannel channel, CancellationToken ct);
}
