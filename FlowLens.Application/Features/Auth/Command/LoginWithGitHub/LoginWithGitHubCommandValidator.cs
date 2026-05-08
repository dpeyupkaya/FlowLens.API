using FluentValidation;

namespace FlowLens.Application.Features.Auth.Commands.LoginWithGitHub
{
    public class LoginWithGitHubCommandValidator : AbstractValidator<LoginWithGitHubCommand>
    {
        public LoginWithGitHubCommandValidator()
        {
            RuleFor(x => x.Code)
                .NotEmpty().WithMessage("GitHub doğrulama kodu (code) bulunamadı.")
                .MinimumLength(10).WithMessage("Geçersiz doğrulama kodu formatı.")
                .MaximumLength(255).WithMessage("Doğrulama kodu çok uzun.");

        }
    }
}