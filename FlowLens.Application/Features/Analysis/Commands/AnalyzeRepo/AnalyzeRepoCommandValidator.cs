using FluentValidation;

namespace FlowLens.Application.Features.Analysis.Commands.AnalyzeRepo
{
    public class AnalyzeRepoCommandValidator : AbstractValidator<AnalyzeRepoCommand>
    {
        public AnalyzeRepoCommandValidator()
        {
            RuleFor(x => x.RepoUrl)
                .NotEmpty().WithMessage("Depo adresi boş bırakılamaz.")
                .Matches(@"^(https?:\/\/)?(www\.)?github\.com\/[a-zA-Z0-9-]+\/[a-zA-Z0-9_.-]+(\.git)?(\/)?$")
                .WithMessage("Lütfen geçerli bir GitHub depo adresi girin. (Örn: https://github.com/facebook/react)");

            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Oturum bilgisi doğrulanamadı. Lütfen giriş yaptığınızdan emin olun.");

            RuleFor(x => x.MaxDepth)
                .InclusiveBetween(1, 10)
                .When(x => x.MaxDepth.HasValue)
                .WithMessage("Analiz derinliği en az 1, en fazla 3 olabilir. Sunucu performansı için daha derine inilemez.");

            RuleFor(x => x.TimezoneOffsetMinutes)
                .InclusiveBetween(-1440, 1440)
                .WithMessage("Geçersiz saat dilimi bilgisi.");

            RuleForEach(x => x.IgnoredFolders)
                .NotEmpty().WithMessage("Göz ardı edilecek klasör isimleri boş olamaz.")
                .When(x => x.IgnoredFolders != null && x.IgnoredFolders.Any());
        }
    }
}