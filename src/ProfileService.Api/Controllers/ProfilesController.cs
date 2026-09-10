using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Options;
using ProfileService.Application.DTOs;
using ProfileService.Application.Interfaces;
using ProfileService.Application.Options;

namespace ProfileService.Api.Controllers;

[ApiController]
[Route("api/v1/profiles")]
public sealed class ProfilesController : ControllerBase
{
    private readonly IProfileService _profiles;
    private readonly IOptions<InternalAuthOptions> _internalAuth;

    public ProfilesController(IProfileService profiles, IOptions<InternalAuthOptions> internalAuth)
    {
        _profiles = profiles;
        _internalAuth = internalAuth;
    }

    /// <summary>ایجاد/همگام‌سازی پروفایل پس از لاگین SSO (فقط ApiKey سرویس‌ها).</summary>
    [HttpPost("ensure")]
    public async Task<ActionResult<ProfileResponse>> Ensure([FromBody] EnsureProfileRequest request, CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        var result = await _profiles.EnsureFromSsoAsync(request, caller, ct);
        return Ok(result);
    }

    /// <summary>دریافت پروفایل بر اساس کد ملی (کاربر فقط خودش؛ سرویس‌ها با ApiKey).</summary>
    [HttpGet("{nationalCode}")]
    public async Task<ActionResult<ProfileResponse>> GetByNationalCode(string nationalCode, CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        var result = await _profiles.GetByNationalCodeAsync(nationalCode, caller, ct);
        return Ok(result);
    }

    [HttpGet("me")]
    public async Task<ActionResult<ProfileResponse>> GetMe(CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        var result = await _profiles.GetMeAsync(caller, ct);
        return Ok(result);
    }

    [HttpPatch("me")]
    public async Task<ActionResult<ProfileResponse>> UpdateMe([FromBody] UpdateProfileRequest request, CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        var result = await _profiles.UpdateMeAsync(request, caller, ct);
        return Ok(result);
    }

    /// <summary>ثبت مرجع آواتار از میکروسرویس فایل‌ها (FileId + ShortCode).</summary>
    [HttpPut("me/avatar")]
    public async Task<ActionResult<ProfileResponse>> SetAvatar([FromBody] SetFileRefRequest request, CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        var result = await _profiles.SetAvatarAsync(request, caller, ct);
        return Ok(result);
    }

    /// <summary>ثبت مرجع تصویر شخص از میکروسرویس فایل‌ها.</summary>
    [HttpPut("me/photo")]
    public async Task<ActionResult<ProfileResponse>> SetPhoto([FromBody] SetFileRefRequest request, CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        var result = await _profiles.SetPhotoAsync(request, caller, ct);
        return Ok(result);
    }

    [HttpPost("me/contacts")]
    public async Task<ActionResult<ContactResponse>> AddContact([FromBody] AddContactRequest request, CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        var result = await _profiles.AddContactAsync(request, caller, ct);
        return Ok(result);
    }

    [HttpDelete("me/contacts/{contactId:guid}")]
    public async Task<IActionResult> RemoveContact(Guid contactId, CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        await _profiles.RemoveContactAsync(contactId, caller, ct);
        return NoContent();
    }

    [HttpPost("me/contacts/{contactId:guid}/verify/send")]
    public async Task<ActionResult<SendVerificationResponse>> SendVerification(Guid contactId, CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        var result = await _profiles.SendVerificationAsync(contactId, caller, ct);
        return Ok(result);
    }

    [HttpPost("me/contacts/{contactId:guid}/verify/confirm")]
    public async Task<ActionResult<ContactResponse>> ConfirmVerification(
        Guid contactId, [FromBody] ConfirmVerificationRequest request, CancellationToken ct)
    {
        var caller = await ResolveAsync(ct);
        var result = await _profiles.ConfirmVerificationAsync(contactId, request, caller, ct);
        return Ok(result);
    }

    private Task<CallerContext> ResolveAsync(CancellationToken ct)
        => _profiles.ResolveCallerAsync(AuthHeader(), ApiKeyHeader(), ct);

    private string? AuthHeader()
        => Request.Headers.Authorization.FirstOrDefault();

    private string? ApiKeyHeader()
    {
        var name = _internalAuth.Value.HeaderName;
        return Request.Headers.TryGetValue(name, out var values) ? values.FirstOrDefault() : null;
    }
}
