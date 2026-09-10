using FluentValidation;
using ProfileService.Application.DTOs;
using ProfileService.Domain.Enums;

namespace ProfileService.Application.Validation;

public sealed class EnsureProfileRequestValidator : AbstractValidator<EnsureProfileRequest>
{
    public EnsureProfileRequestValidator()
    {
        RuleFor(x => x.NationalCode)
            .NotEmpty().WithMessage("National code is required.")
            .Matches(@"^\d{10}$").WithMessage("National code must be exactly 10 digits.");

        RuleFor(x => x.Mobile)
            .Matches(@"^09\d{9}$")
            .When(x => !string.IsNullOrWhiteSpace(x.Mobile))
            .WithMessage("Mobile must be a valid Iranian mobile number (09xxxxxxxxx).");
    }
}

public sealed class UpdateProfileRequestValidator : AbstractValidator<UpdateProfileRequest>
{
    public UpdateProfileRequestValidator()
    {
        RuleFor(x => x.UserBirthDate)
            .MaximumLength(32)
            .When(x => x.UserBirthDate is not null);
    }
}

public sealed class SetFileRefRequestValidator : AbstractValidator<SetFileRefRequest>
{
    public SetFileRefRequestValidator()
    {
        RuleFor(x => x.FileId).NotEmpty();
        RuleFor(x => x.ShortCode).NotEmpty().MaximumLength(32);
        RuleFor(x => x.Url).MaximumLength(512).When(x => x.Url is not null);
    }
}

public sealed class AddContactRequestValidator : AbstractValidator<AddContactRequest>
{
    public AddContactRequestValidator()
    {
        RuleFor(x => x.Type).IsInEnum();
        RuleFor(x => x.Number).NotEmpty();

        RuleFor(x => x.Number)
            .Matches(@"^09\d{9}$")
            .When(x => x.Type == ContactType.Mobile)
            .WithMessage("Mobile must be a valid Iranian mobile number (09xxxxxxxxx).");

        RuleFor(x => x.Number)
            .Matches(@"^0\d{10,11}$")
            .When(x => x.Type == ContactType.Landline)
            .WithMessage("Landline must be a valid Iranian phone (0xxxxxxxxxx).");
    }
}

public sealed class ConfirmVerificationRequestValidator : AbstractValidator<ConfirmVerificationRequest>
{
    public ConfirmVerificationRequestValidator()
    {
        RuleFor(x => x.Code)
            .NotEmpty()
            .Matches(@"^\d{4,8}$")
            .WithMessage("Verification code is invalid.");
    }
}
