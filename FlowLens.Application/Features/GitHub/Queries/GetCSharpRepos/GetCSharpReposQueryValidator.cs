using FluentValidation;

namespace FlowLens.Application.Features.GitHub.Queries.GetCSharpRepos
{
    public class GetCSharpReposQueryValidator : AbstractValidator<GetCSharpReposQuery>
    {
        public GetCSharpReposQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanıcı kimliği doğrulanamadı.");

        }
    }
}