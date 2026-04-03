using FluentValidation;

namespace FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo;

public class AnalyzeRepoCommandValidator : AbstractValidator<AnalyzeRepoCommand>
{
    public AnalyzeRepoCommandValidator()
    {
        RuleFor(x => x.RepoUrl)
            .NotEmpty().WithMessage(" Repo URL'si boş olamaz!")
            .Must(uri => Uri.TryCreate(uri, UriKind.Absolute, out _)).WithMessage("Geçerli bir URL girmen lazım.")
            .Must(uri => uri.Contains("github.com")).WithMessage("Şimdilik sadece GitHub repolarını alabiliyorum.");

        RuleFor(x => x.AccessToken)
            .MaximumLength(100).WithMessage("Token çok uzun değil mi?");
    }
}