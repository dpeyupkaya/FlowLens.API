using FluentValidation;

namespace FlowLens.Application.Features.GitHub.Queries.GetUserRepos
{
    public class GetUserReposQueryValidator : AbstractValidator<GetUserReposQuery>
    {
        public GetUserReposQueryValidator()
        {
            RuleFor(x => x.UserId)
                .NotEmpty().WithMessage("Kullanıcı kimliği doğrulanamadı.");
        }
    }
}