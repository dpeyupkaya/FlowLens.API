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

        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı ID'si boş olamaz.")
            .Must(id => Guid.TryParse(id.ToString(), out _)).WithMessage("Geçerli bir kullanıcı ID'si girmeniz gerekiyor.");
    }
}