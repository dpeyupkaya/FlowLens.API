using FluentValidation;

namespace FlowLens.Application.Features.Users.Commands.UpdateUserSettings;

public class UpdateUserSettingsCommandValidator : AbstractValidator<UpdateUserSettingsCommand>
{
    public UpdateUserSettingsCommandValidator()
    {
        RuleFor(x => x.UserId)
            .NotEmpty().WithMessage("Kullanıcı kimliği doğrulanmalıdır.");

        RuleFor(x => x.Settings.Analysis)
            .NotNull().WithMessage("Analiz tercihleri boş olamaz.");

        RuleFor(x => x.Settings.Analysis.MaxAnalysisDepth)
            .InclusiveBetween(1, 3).WithMessage("Analiz derinliği en az 1, en fazla 3 olabilir.");

        RuleForEach(x => x.Settings.Analysis.ExcludedFolders)
            .MaximumLength(50).WithMessage("Klasör adı çok uzun.");

        RuleFor(x => x.Settings.Graphics)
            .NotNull().WithMessage("Grafik tercihleri boş olamaz.");

        

        RuleFor(x => x.Settings.Data.RepoVisibility)
            .Must(x => new[] { "All", "Public", "Private" }.Contains(x))
            .WithMessage("Geçersiz depo görünürlük ayarı.");
    }
}